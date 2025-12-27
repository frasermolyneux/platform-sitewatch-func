using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace MX.Platform.SitewatchFunc;

public class ExternalHealthCheck
{
    private readonly IConfiguration configuration;
    private readonly TelemetryClient telemetryClient;
    public Dictionary<string, TelemetryClient> telemetryClients { get; set; } = new Dictionary<string, TelemetryClient>();

    private readonly AsyncRetryPolicy<HttpResponseMessage> retryPolicy;

    public ExternalHealthCheck(IConfiguration configuration, TelemetryClient telemetryClient)
    {
        this.configuration = configuration;
        this.telemetryClient = telemetryClient;

        retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var message = $"Request failed with {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}. Waiting {timespan} before next retry. Retry attempt {retryAttempt}";
                    telemetryClient.TrackException(outcome.Exception ?? new Exception(message));

                    if (outcome.Result != null && !outcome.Result.IsSuccessStatusCode)
                    {
                        telemetryClient.TrackTrace(outcome.Result.Content.ReadAsStringAsync().Result);
                    }
                });
    }

    [Function(nameof(ExternalHealthCheck))]
    public async Task Run([TimerTrigger("0,30 * * * * *")] TimerInfo timer, ILogger log, FunctionContext executionContext)
    {
        var testConfigs = JsonConvert.DeserializeObject<List<TestConfig>>(configuration["test_config"]);

        foreach (var testConfig in testConfigs)
        {
            if (!telemetryClients.ContainsKey(testConfig.AppInsights))
            {
                var telemetryConfiguration = new TelemetryConfiguration
                {
                    ConnectionString = configuration[$"{testConfig.AppInsights.ToLower()}_appinsights_connection_string"],
                    TelemetryChannel = new InMemoryChannel()
                };
                telemetryClients.Add(testConfig.AppInsights, new TelemetryClient(telemetryConfiguration));
            }

            var telemetryClient = telemetryClients[testConfig.AppInsights];
            string location = Environment.GetEnvironmentVariable("REGION_NAME") ?? "Unknown";

            var availability = new AvailabilityTelemetry
            {
                Name = testConfig.App,
                RunLocation = location,
                Success = false,
            };

            availability.Context.Operation.ParentId = Activity.Current.SpanId.ToString();
            availability.Context.Operation.Id = Activity.Current.RootId;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                using (var activity = new Activity("AvailabilityContext"))
                {
                    activity.Start();
                    availability.Id = Activity.Current.SpanId.ToString();
                    await RunAvailabilityTestAsync(log, testConfig.Uri);
                }
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

    private async Task RunAvailabilityTestAsync(ILogger log, string uri)
    {
        var matches = Regex.Matches(uri, @"%([a-zA-Z1-9_]+)%");

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                var token = match.Groups[1].ToString();

                if (configuration[token] == null)
                {
                    throw new Exception($"Token {token} not found in configuration");
                }

                uri = uri.Replace($"%{token}%", configuration[token]);
            }
        }

        using (var httpClient = new HttpClient())
        {
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await retryPolicy.ExecuteAsync(() => httpClient.GetAsync(uri));
            if (!response.IsSuccessStatusCode)
            {
                telemetryClient.TrackTrace(response.Content.ReadAsStringAsync().Result);
                throw new Exception($"Failed to get a successful response from {uri}, received {response.StatusCode}");
            }
        }
    }
}
