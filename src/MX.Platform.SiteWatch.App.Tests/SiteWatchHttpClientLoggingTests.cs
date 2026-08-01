using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MX.Platform.SiteWatch.App.Tests;

public sealed class SiteWatchHttpClientLoggingTests
{
    private static readonly string LogicalHandlerCategory = $"{SiteWatchHttpClient.LoggingCategoryPrefix}LogicalHandler";
    private static readonly string ClientHandlerCategory = $"{SiteWatchHttpClient.LoggingCategoryPrefix}ClientHandler";

    [Fact]
    public async Task NamedClient_WithoutFilter_EmitsExpectedLifecycleCategories()
    {
        using var collector = new CollectingLoggerProvider();
        await using var provider = CreateServiceProvider(collector, configureFilter: false);
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(SiteWatchHttpClient.Name);

        using var response = await client.GetAsync("https://sitewatch.test/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Collection(
            collector.Entries.Where(entry => entry.Level == LogLevel.Information).ToArray(),
            entry => AssertLifecycleEntry(entry, LogicalHandlerCategory, "RequestPipelineStart"),
            entry => AssertLifecycleEntry(entry, ClientHandlerCategory, "RequestStart"),
            entry => AssertLifecycleEntry(entry, ClientHandlerCategory, "RequestEnd"),
            entry => AssertLifecycleEntry(entry, LogicalHandlerCategory, "RequestPipelineEnd"));
    }

    [Fact]
    public async Task NamedClient_WithFilter_SuppressesInformationLifecycleLogs()
    {
        using var collector = new CollectingLoggerProvider();
        await using var provider = CreateServiceProvider(collector, configureFilter: true);
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(SiteWatchHttpClient.Name);

        using var response = await client.GetAsync("https://sitewatch.test/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(
            collector.Entries,
            entry => entry.Category.StartsWith(SiteWatchHttpClient.LoggingCategoryPrefix, StringComparison.Ordinal));
    }

    [Fact]
    public void ConfigureLogging_RetainsWarningsErrorsAndUnrelatedCategories()
    {
        using var collector = new CollectingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(collector);
            SiteWatchHttpClient.ConfigureLogging(logging);
        });
        var logicalHandlerLogger = loggerFactory.CreateLogger(LogicalHandlerCategory);
        var externalHealthCheckLogger = loggerFactory.CreateLogger<ExternalHealthCheck>();
        var functionWorkerLogger = loggerFactory.CreateLogger("Microsoft.Azure.Functions.Worker.Invocation");
        var unrelatedApplicationLogger = loggerFactory.CreateLogger("MX.Platform.SiteWatch.App.OtherService");
        var similarlyPrefixedClientLogger = loggerFactory.CreateLogger(
            "System.Net.Http.HttpClient.SiteWatchCanary.LogicalHandler");

        logicalHandlerLogger.LogInformation("routine lifecycle");
        logicalHandlerLogger.LogWarning("retained framework warning");
        logicalHandlerLogger.LogError("retained framework error");
        externalHealthCheckLogger.LogWarning("retained retry warning");
        externalHealthCheckLogger.LogError("retained terminal error");
        functionWorkerLogger.LogInformation("retained function worker information");
        unrelatedApplicationLogger.LogInformation("retained application information");
        similarlyPrefixedClientLogger.LogInformation("retained similarly prefixed client information");

        Assert.DoesNotContain(collector.Entries, entry => entry.Message == "routine lifecycle");
        Assert.Contains(collector.Entries, entry => entry.Message == "retained framework warning");
        Assert.Contains(collector.Entries, entry => entry.Message == "retained framework error");
        Assert.Contains(collector.Entries, entry => entry.Message == "retained retry warning");
        Assert.Contains(collector.Entries, entry => entry.Message == "retained terminal error");
        Assert.Contains(collector.Entries, entry => entry.Message == "retained function worker information");
        Assert.Contains(collector.Entries, entry => entry.Message == "retained application information");
        Assert.Contains(collector.Entries, entry => entry.Message == "retained similarly prefixed client information");
    }

    private static ServiceProvider CreateServiceProvider(CollectingLoggerProvider collector, bool configureFilter)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(collector);
            if (configureFilter)
            {
                SiteWatchHttpClient.ConfigureLogging(logging);
            }
        });
        services.AddHttpClient(SiteWatchHttpClient.Name)
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler());
        return services.BuildServiceProvider();
    }

    private static void AssertLifecycleEntry(CapturedLogEntry entry, string category, string eventName)
    {
        Assert.Equal(category, entry.Category);
        Assert.Equal(eventName, entry.EventId.Name);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
            });
        }
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        private readonly List<CapturedLogEntry> entries = [];

        public IReadOnlyList<CapturedLogEntry> Entries
        {
            get
            {
                lock (entries)
                {
                    return [.. entries];
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CollectingLogger(categoryName, entries);
        }

        public void Dispose()
        {
        }
    }

    private sealed class CollectingLogger(string category, List<CapturedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (entries)
            {
                entries.Add(new CapturedLogEntry(category, logLevel, eventId, formatter(state, exception)));
            }
        }
    }

    private sealed record CapturedLogEntry(
        string Category,
        LogLevel Level,
        EventId EventId,
        string Message);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
