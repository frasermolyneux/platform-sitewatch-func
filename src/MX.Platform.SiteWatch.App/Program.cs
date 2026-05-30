using System.Reflection;
using System.Text.Json;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MX.Observability.OpenTelemetry.Availability;
using MX.Observability.OpenTelemetry.WorkerService;
using MX.Platform.SiteWatch.App;
using MX.Platform.SiteWatch.App.Availability;

const string CanonicalAiConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";
const string ServiceName = "Sitewatch FuncApp";

var host = new HostBuilder()
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        // Enforce single canonical app setting for Application Insights connection string.
        var connectionString = config[CanonicalAiConnectionStringKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Required app setting '{CanonicalAiConnectionStringKey}' is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")))
        {
            Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        }

        services.AddHttpClient("SiteWatch", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddLogging();
        services.AddObservability();
        services.AddHealthChecks();

        services.Configure<SiteWatchOptions>(config.GetSection("SiteWatch"));

        services.PostConfigure<SiteWatchOptions>(options =>
        {
            if (options.Tests.Count == 0)
            {
                var rawConfig = config["test_config"];
                if (!string.IsNullOrWhiteSpace(rawConfig))
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };
                    options.Tests = JsonSerializer.Deserialize<List<TestConfig>>(rawConfig, jsonOptions) ?? [];
                }
            }
        });

        // Build the SiteWatch-local multi-target availability fan-out: each test's `app_insights`
        // value routes its availability entry to a named Application Insights connection string.
        // Entries with an unknown or "default" target fall back to the host's own Application
        // Insights sink (the one wired by AddObservability()).
        var targets = new AvailabilityTelemetryTargets();
        foreach (var child in config.GetSection("SiteWatch:Telemetry").GetChildren())
        {
            if (string.IsNullOrWhiteSpace(child.Key) || string.IsNullOrWhiteSpace(child.Value))
            {
                continue;
            }

            targets.Targets[child.Key] = child.Value;
        }

        // Replace the default IAvailabilityTelemetry singleton registered by AddObservability()
        // with the SiteWatch multi-target router.
        services.RemoveAll<IAvailabilityTelemetry>();
        services.AddSingleton<IAvailabilityTelemetry>(sp => new MultiTargetAvailabilityTelemetry(
            sp.GetRequiredService<ILoggerFactory>(),
            targets,
            ServiceName));
    })
    .Build();

await host.RunAsync();
