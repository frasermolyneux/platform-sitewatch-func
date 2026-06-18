using System.Diagnostics;
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
    // Cap the number of concurrent availability checks per timer tick. Tuned so that even with a
    // worst-case retry sequence (4 attempts x 5s timeout + 2/4/8s backoff ~= 34s per failing test),
    // a single batch with a handful of healthy tests does not stall the whole tick. Keep this in
    // mind when adding tests: total wall-clock per tick is roughly ceil(tests / 5) * worst-case.
    private const int MaxConcurrentChecks = 5;

    private readonly IConfiguration configuration;
    private readonly IOptionsMonitor<SiteWatchOptions> optionsMonitor;
    private readonly HttpClient httpClient;
    private readonly IAvailabilityTelemetry availabilityTelemetry;

    public ExternalHealthCheck(
        IConfiguration configuration,
        IOptionsMonitor<SiteWatchOptions> optionsMonitor,
        IHttpClientFactory httpClientFactory,
        IAvailabilityTelemetry availabilityTelemetry)
    {
        this.configuration = configuration;
        this.optionsMonitor = optionsMonitor;
        this.httpClient = httpClientFactory.CreateClient("SiteWatch");
        this.availabilityTelemetry = availabilityTelemetry;
    }

    [Function(nameof(ExternalHealthCheck))]
    public async Task Run([TimerTrigger("0,30 * * * * *")] TimerInfo timer, ILogger log, FunctionContext executionContext)
    {
        var options = optionsMonitor.CurrentValue;

        if (options.DisableExternalChecks)
        {
            log.LogInformation("External checks disabled by configuration; skipping run.");
            return;
        }

        var testConfigs = options.Tests ?? [];

        if (testConfigs.Count == 0)
        {
            log.LogInformation("No availability tests configured; skipping run.");
            return;
        }

        var location = Environment.GetEnvironmentVariable("REGION_NAME") ?? "Unknown";
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrentChecks,
            CancellationToken = executionContext.CancellationToken,
        };

        await Parallel.ForEachAsync(
            testConfigs,
            parallelOptions,
            (testConfig, ct) => ExecuteTestAsync(testConfig, location, log, ct));
    }

    private async ValueTask ExecuteTestAsync(TestConfig testConfig, string location, ILogger log, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = new Activity("AvailabilityCheck");
        activity.AddTag("app", testConfig.App);
        activity.AddTag("location", location);
        activity.Start();

        try
        {
            var uri = ReplaceTokens(testConfig.Uri, configuration);
            await RunAvailabilityTestAsync(log, uri, cancellationToken);

            stopwatch.Stop();
            availabilityTelemetry.Track(new AvailabilityTelemetryEntry
            {
                Name = testConfig.App,
                Success = true,
                Duration = stopwatch.Elapsed,
                RunLocation = location,
                Message = "OK",
                Target = testConfig.AppInsights,
                Properties = new Dictionary<string, string>
                {
                    ["component"] = string.IsNullOrWhiteSpace(testConfig.Component) ? testConfig.App : testConfig.Component
                }
            });

            log.LogInformation(
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
            log.LogInformation(
                "Availability check for '{App}' at '{Location}' cancelled by host after {Duration}ms",
                testConfig.App,
                location,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            availabilityTelemetry.Track(new AvailabilityTelemetryEntry
            {
                Name = testConfig.App,
                Success = false,
                Duration = stopwatch.Elapsed,
                RunLocation = location,
                Message = ex.Message,
                Target = testConfig.AppInsights,
                Properties = new Dictionary<string, string>
                {
                    ["component"] = string.IsNullOrWhiteSpace(testConfig.Component) ? testConfig.App : testConfig.Component
                }
            });

            log.LogError(
                ex,
                "Availability check failed for '{App}' at '{Location}' after {Duration}ms: {Message}",
                testConfig.App,
                location,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
        }
        finally
        {
            activity.Stop();
        }
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

    private async Task RunAvailabilityTestAsync(ILogger log, string uri, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1 << retryAttempt),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    if (outcome.Exception is not null)
                    {
                        log.LogWarning(
                            outcome.Exception,
                            "Request retry {RetryAttempt}: {ExceptionType} - waiting {WaitTime}",
                            retryAttempt,
                            outcome.Exception.GetType().Name,
                            timespan);
                    }
                    else if (outcome.Result is not null)
                    {
                        log.LogWarning(
                            "Request retry {RetryAttempt}: status {StatusCode} - waiting {WaitTime}",
                            retryAttempt,
                            outcome.Result.StatusCode,
                            timespan);
                    }
                });

        var response = await retryPolicy.ExecuteAsync(
            ct => httpClient.GetAsync(uri, ct),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            log.LogError(
                "Failed to get a successful response from {Uri}, received {StatusCode}: {Content}",
                uri,
                response.StatusCode,
                content);
            throw new Exception($"Failed to get a successful response from {uri}, received {response.StatusCode}");
        }
    }

    [GeneratedRegex(@"%([a-zA-Z0-9_]+)%")]
    private static partial Regex TokenPattern();
}
