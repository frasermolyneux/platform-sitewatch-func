using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MX.Observability.OpenTelemetry.Availability;
using Polly;

namespace MX.Platform.SiteWatch.App;

public partial class ExternalHealthCheck
{
    // Sleep-duration provider for the terminal Polly retry policy. Kept as an internal seam so
    // tests can drive the retry pipeline in zero real time without altering production behaviour;
    // production wiring keeps the exponential 2s/4s/8s progression.
    internal Func<int, TimeSpan> RetryBackoff { get; set; } = retryAttempt => TimeSpan.FromSeconds(1 << retryAttempt);

    // Cap the number of concurrent availability checks per timer tick. Tuned so that even with a
    // worst-case retry sequence (4 attempts x 5s timeout + 2/4/8s backoff ~= 34s per failing test),
    // a single batch with a handful of healthy tests does not stall the whole tick. Keep this in
    // mind when adding tests: total wall-clock per tick is roughly ceil(tests / 5) * worst-case.
    private const int MaxConcurrentChecks = 5;

    // Sentinel value emitted as the `region` custom dimension when REGION_NAME is unavailable. Kept
    // distinct from any real Azure region string so consumers never mistake a misconfigured probe
    // for a healthy report from an expected region (see platform-status-web regional aggregation).
    private const string UnknownRegion = "unknown";

    private readonly IConfiguration configuration;
    private readonly IOptionsMonitor<SiteWatchOptions> optionsMonitor;
    private readonly HttpClient httpClient;
    private readonly IAvailabilityTelemetry availabilityTelemetry;
    private readonly ILogger<ExternalHealthCheck> logger;

    public ExternalHealthCheck(
        IConfiguration configuration,
        IOptionsMonitor<SiteWatchOptions> optionsMonitor,
        IHttpClientFactory httpClientFactory,
        IAvailabilityTelemetry availabilityTelemetry,
        ILogger<ExternalHealthCheck> logger)
    {
        this.configuration = configuration;
        this.optionsMonitor = optionsMonitor;
        httpClient = httpClientFactory.CreateClient(SiteWatchHttpClient.Name);
        this.availabilityTelemetry = availabilityTelemetry;
        this.logger = logger;
    }

    [Function(nameof(ExternalHealthCheck))]
    public async Task Run([TimerTrigger("0,30 * * * * *")] TimerInfo timer, FunctionContext executionContext)
    {
        var options = optionsMonitor.CurrentValue;

        if (options.DisableExternalChecks)
        {
            logger.LogInformation("External checks disabled by configuration; skipping run.");
            return;
        }

        var testConfigs = options.Tests ?? [];

        if (testConfigs.Count == 0)
        {
            logger.LogInformation("No availability tests configured; skipping run.");
            return;
        }

        var location = Environment.GetEnvironmentVariable("REGION_NAME");
        if (string.IsNullOrWhiteSpace(location))
        {
            logger.LogWarning("REGION_NAME app setting is missing or empty; emitting '{UnknownRegion}' region dimension for this tick.", UnknownRegion);
            location = UnknownRegion;
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrentChecks,
            CancellationToken = executionContext.CancellationToken,
        };

        await Parallel.ForEachAsync(
            testConfigs,
            parallelOptions,
            (testConfig, ct) => ExecuteTestAsync(testConfig, location, ct));
    }

    private async ValueTask ExecuteTestAsync(TestConfig testConfig, string location, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = new Activity("AvailabilityCheck");
        activity.AddTag("app", testConfig.App);
        activity.AddTag("location", location);
        activity.Start();

        try
        {
            var uri = ReplaceTokens(testConfig.Uri, configuration);
            await RunAvailabilityTestAsync(testConfig, uri, cancellationToken);

            stopwatch.Stop();
            availabilityTelemetry.Track(new AvailabilityTelemetryEntry
            {
                Name = testConfig.App,
                Success = true,
                Duration = stopwatch.Elapsed,
                RunLocation = location,
                Message = "OK",
                Target = testConfig.AppInsights,
                Properties = BuildContractDimensions(testConfig, location)
            });

            logger.LogInformation(
                "Availability check passed for '{App}' at '{Location}' in {Duration}ms",
                testConfig.App,
                location,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host shutdown (or parent Parallel.ForEachAsync cancellation): the token was signalled
            // externally, so this is not a real availability failure. Skip the telemetry write to
            // avoid contaminating availabilityResults with spurious failures during graceful exit.
            // Note: HttpClient timeout throws TaskCanceledException with IsCancellationRequested=false
            // on the supplied token, so timeout failures still flow through the catch below.
            logger.LogInformation(
                "Availability check for '{App}' at '{Location}' cancelled by host after {Duration}ms",
                testConfig.App,
                location,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var failureSummary = ClassifyFailure(ex);
            availabilityTelemetry.Track(new AvailabilityTelemetryEntry
            {
                Name = testConfig.App,
                Success = false,
                Duration = stopwatch.Elapsed,
                RunLocation = location,
                Message = failureSummary,
                Target = testConfig.AppInsights,
                Properties = BuildContractDimensions(testConfig, location)
            });

            logger.LogError(
                ex,
                "Availability check failed for '{App}' at '{Location}' after {Duration}ms: {FailureSummary}",
                testConfig.App,
                location,
                stopwatch.ElapsedMilliseconds,
                failureSummary);
        }
        finally
        {
            activity.Stop();
        }
    }

    /// <summary>
    /// Builds the explicit, stable telemetry contract dimensions emitted on every availability result:
    /// <c>componentId</c> (stable component identifier, falling back to <see cref="TestConfig.App"/>),
    /// <c>siteId</c> (the tenant/site identifier, required on <see cref="TestConfig"/>), and
    /// <c>region</c> (the resolved probe region). Consumers (platform-status-web) must filter on these
    /// explicit dimensions rather than inferring tenant from a component-name prefix at query time.
    /// </summary>
    internal static Dictionary<string, string> BuildContractDimensions(TestConfig testConfig, string location)
    {
        return new Dictionary<string, string>
        {
            ["componentId"] = string.IsNullOrWhiteSpace(testConfig.Component) ? testConfig.App : testConfig.Component,
            ["siteId"] = testConfig.Site,
            ["region"] = location,
        };
    }

    private static string ReplaceTokens(string uriTemplate, IConfiguration configuration)
    {
        var uri = uriTemplate;
        var matches = TokenPattern().Matches(uri);

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                var token = match.Groups[1].Value;

                if (configuration[token] == null)
                {
                    throw new Exception($"Token '{token}' not found in configuration.");
                }

                uri = uri.Replace($"%{token}%", configuration[token]);
            }
        }

        return uri;
    }

    private async Task RunAvailabilityTestAsync(TestConfig testConfig, string uri, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, RetryBackoff,
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    if (outcome.Exception is not null)
                    {
                        // Deliberately log only the exception TYPE and app identifier: HttpClient
                        // exceptions can carry host/URL fragments in ex.Message which must not
                        // reach telemetry per the telemetry cost-optimisation safety gate.
                        logger.LogWarning(
                            "Request retry {RetryAttempt} for '{App}': {ExceptionType} - waiting {WaitTime}",
                            retryAttempt,
                            testConfig.App,
                            outcome.Exception.GetType().Name,
                            timespan);
                    }
                    else if (outcome.Result is not null)
                    {
                        var statusCode = (int)outcome.Result.StatusCode;
                        logger.LogWarning(
                            "Request retry {RetryAttempt} for '{App}': status {StatusCode} ({StatusClass}) - waiting {WaitTime}",
                            retryAttempt,
                            testConfig.App,
                            statusCode,
                            StatusClass(statusCode),
                            timespan);

                        // Dispose the failing intermediate response now: Polly discards it before
                        // executing the next attempt, and its body may contain sensitive content
                        // that must not linger in the connection pool.
                        outcome.Result.Dispose();
                    }
                });

        using var response = await retryPolicy.ExecuteAsync(
            ct => httpClient.GetAsync(uri, ct),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var statusClass = StatusClass(statusCode);
            logger.LogError(
                "Availability check '{App}' terminal failure: HTTP {StatusCode} ({StatusClass})",
                testConfig.App,
                statusCode,
                statusClass);

            // Sanitised terminal exception: NO URI, query string, tokens, or response body.
            // Setting StatusCode preserves programmatic classification for downstream consumers.
            throw new HttpRequestException(
                $"Availability check '{testConfig.App}' failed with HTTP {statusCode} ({statusClass}).",
                inner: null,
                statusCode: response.StatusCode);
        }
    }

    /// <summary>
    /// Produces a short, sanitised classification of an availability-check failure suitable for
    /// both the outer error log and the <c>AvailabilityTelemetryEntry.Message</c>. Only exception
    /// type and HTTP status class are exposed; the raw <c>ex.Message</c> is never surfaced so
    /// HttpClient-generated URL/host fragments cannot leak into telemetry consumers.
    /// </summary>
    internal static string ClassifyFailure(Exception ex)
    {
        return ex switch
        {
            HttpRequestException hre when hre.StatusCode is HttpStatusCode statusCode
                => $"HTTP {(int)statusCode} ({StatusClass((int)statusCode)})",
            HttpRequestException => "HttpRequestException",
            TaskCanceledException => "Timeout",
            OperationCanceledException => "Cancelled",
            _ => ex.GetType().Name,
        };
    }

    private static string StatusClass(int statusCode)
    {
        return $"{statusCode / 100}xx";
    }

    [GeneratedRegex(@"%([a-zA-Z0-9_]+)%")]
    private static partial Regex TokenPattern();
}
