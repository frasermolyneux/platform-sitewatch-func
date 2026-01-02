using System.Reflection;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

using MX.Platform.SitewatchFunc;

var host = new HostBuilder()
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddLogging();
        services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHealthChecks();

        services.Configure<SiteWatchOptions>(config.GetSection("SiteWatch"));

        services.PostConfigure<SiteWatchOptions>(options =>
        {
            if (options.Tests.Count == 0)
            {
                var rawConfig = config["test_config"];
                if (!string.IsNullOrWhiteSpace(rawConfig))
                {
                    options.Tests = JsonConvert.DeserializeObject<List<TestConfig>>(rawConfig) ?? new();
                }
            }

            if (options.Telemetry.Count == 0)
            {
                foreach (var entry in config.AsEnumerable())
                {
                    if (string.IsNullOrEmpty(entry.Key) || string.IsNullOrEmpty(entry.Value))
                    {
                        continue;
                    }

                    if (entry.Key.EndsWith("_appinsights_connection_string", StringComparison.OrdinalIgnoreCase))
                    {
                        var key = entry.Key[..^"_appinsights_connection_string".Length];
                        options.Telemetry[key] = entry.Value;
                    }
                }

                var defaultConnection =
                    config["ApplicationInsights:ConnectionString"] ??
                    config["APPINSIGHTS_CONNECTIONSTRING"];

                if (!string.IsNullOrWhiteSpace(defaultConnection) && !options.Telemetry.ContainsKey("default"))
                {
                    options.Telemetry["default"] = defaultConnection;
                }
            }
        });
    })
    .Build();

await host.RunAsync();
