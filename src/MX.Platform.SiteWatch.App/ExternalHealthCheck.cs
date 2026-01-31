using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MX.Platform.SitewatchFunc;

public partial class ExternalHealthCheck
{
    private readonly IConfiguration configuration;
    private readonly IOptionsMonitor<SiteWatchOptions> optionsMonitor;
    private readonly TelemetryClient telemetryClient;
    public Dictionary<string, TelemetryClient> telemetryClients { get; set; } = [];

    private readonly AsyncRetryPolicy<HttpResponseMessage> retryPolicy;

    public ExternalHealthCheck(IConfiguration configuration, TelemetryClient telemetryClient, IOptionsMonitor<SiteWatchOptions> optionsMonitor)
    {
        this.configuration = configuration;
        this.telemetryClient = telemetryClient;
        this.optionsMonitor = optionsMonitor;

        retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1 << retryAttempt),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var message = $"Request failed with {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}. Waiting {timespan} before next retry. Retry attempt {retryAttempt}";
                    telemetryClient.TrackException(outcome.Exception ?? new Exception(message));

                    if (outcome.Result != null && !outcome.Result.IsSuccessStatusCode)
                    {
                        telemetryClient.TrackTrace(outcome.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    }
                });
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

        foreach (var testConfig in testConfigs)
        {
            var telemetryClient = GetTelemetryClient(options, testConfig.AppInsights);

            if (telemetryClient == null)
            {
                log.LogWarning("No telemetry connection configured for app insights key '{AppInsights}'. Skipping test '{App}'.", testConfig.AppInsights, testConfig.App);
                continue;
            }
            string location = Environment.GetEnvironmentVariable("REGION_NAME") ?? "Unknown";

            var availability = new AvailabilityTelemetry
            {
                Name = testConfig.App,
                RunLocation = location,
                Success = false,
            };

            availability.Context.Operation.ParentId = Activity.Current?.SpanId.ToString();
            availability.Context.Operation.Id = Activity.Current?.RootId;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var activity = new Activity("AvailabilityContext");
                activity.Start();
                availability.Id = Activity.Current?.SpanId.ToString();
                await RunAvailabilityTestAsync(log, testConfig.Uri);
                availability.Success = true;
            }
            catch (Exception ex)
            {
                availability.Message = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                availability.Duration = stopwatch.Elapsed;
                availability.Timestamp = DateTimeOffset.UtcNow;
                telemetryClient.TrackAvailability(availability);
                telemetryClient.Flush();
            }

        }
    }

    private TelemetryClient? GetTelemetryClient(SiteWatchOptions options, string appInsightsKey)
    {
        var key = string.IsNullOrWhiteSpace(appInsightsKey) ? "default" : appInsightsKey;

        if (!telemetryClients.TryGetValue(key, out var client))
        {
            if (!options.Telemetry.TryGetValue(key, out var connectionString))
            {
                if (!options.Telemetry.TryGetValue("default", out connectionString))
                {
                    return null;
                }
            }

            var telemetryConfiguration = new TelemetryConfiguration
            {
                ConnectionString = connectionString,
                TelemetryChannel = new InMemoryChannel(),
            };

            client = new TelemetryClient(telemetryConfiguration);
            telemetryClients.Add(key, client);
        }

        return client;
    }

    private async Task RunAvailabilityTestAsync(ILogger log, string uri)
    {
        var matches = TokenPattern().Matches(uri);

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                var token = match.Groups[1].Value;

                if (configuration[token] == null)
                {
                    throw new Exception($"Token {token} not found in configuration");
                }

                uri = uri.Replace($"%{token}%", configuration[token]);
            }
        }

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        var response = await retryPolicy.ExecuteAsync(() => httpClient.GetAsync(uri));
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            telemetryClient.TrackTrace(content);
            throw new Exception($"Failed to get a successful response from {uri}, received {response.StatusCode}");
        }
    }

    [GeneratedRegex(@"%([a-zA-Z0-9_]+)%")]
    private static partial Regex TokenPattern();
}
