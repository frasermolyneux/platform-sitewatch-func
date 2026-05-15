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
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer, ILogger log, FunctionContext executionContext)
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

        foreach (var testConfig in testConfigs)
        {
            string location = Environment.GetEnvironmentVariable("REGION_NAME") ?? "Unknown";
            var stopwatch = Stopwatch.StartNew();
            using var activity = new Activity("AvailabilityCheck");
            activity.AddTag("app", testConfig.App);
            activity.AddTag("location", location);
            activity.Start();

            try
            {
                var uri = ReplaceTokens(testConfig.Uri, configuration);
                await RunAvailabilityTestAsync(log, uri);

                stopwatch.Stop();
                availabilityTelemetry.Track(new AvailabilityTelemetryEntry
                {
                    Name = testConfig.App,
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    RunLocation = location,
                    Message = "OK",
                    Target = testConfig.AppInsights
                });

                log.LogInformation(
                    "Availability check passed for '{App}' at '{Location}' in {Duration}ms",
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
                    Target = testConfig.AppInsights
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

    private async Task RunAvailabilityTestAsync(ILogger log, string uri)
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

        var response = await retryPolicy.ExecuteAsync(() => httpClient.GetAsync(uri));
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
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
