using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MX.Observability.OpenTelemetry.Availability;

namespace MX.Platform.SiteWatch.App.Tests;

/// <summary>
/// Safety-gate tests for <see cref="ExternalHealthCheck"/> covering the sanitised terminal
/// failure path required by Phase 0 Part 2 of the shared telemetry cost-optimisation work
/// package: response bodies must never be logged, expanded URIs/tokens/query strings must not
/// appear in captured logs or thrown exception messages, and the availability entry
/// <c>Message</c> must be a sanitised classification. Also verifies that the pre-existing retry
/// count, cancellation, and timeout behaviour is preserved.
/// </summary>
public sealed class ExternalHealthCheckTests
{
    private const string SecretQueryValue = "supersecrettokenshouldneverleak";
    private const string SensitiveResponseBody = "SENSITIVE_RESPONSE_BODY_PAYLOAD_DO_NOT_LEAK";
    private const string AppName = "app-sentinel-prd";
    private const string SiteId = "xi";
    private const string HostName = "example.invalid";
    private const string PathSegment = "/probe";
    private const string ExpandedUri = "https://" + HostName + PathSegment + "?key=" + SecretQueryValue;

    [Fact]
    public async Task Success_TracksSuccessAvailability_WithoutRetryWarnings()
    {
        var stub = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok"),
        });
        using var harness = new Harness(stub);

        await harness.ExecuteAsync(NewTestConfig());

        Assert.Equal(1, stub.CallCount);
        Assert.Single(harness.Spy.Entries);
        Assert.True(harness.Spy.Entries[0].Success);
        Assert.Equal("OK", harness.Spy.Entries[0].Message);
        Assert.DoesNotContain(harness.LogEntries, e => e.Level == LogLevel.Warning);
        Assert.DoesNotContain(harness.LogEntries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task NonSuccessTerminal_EmitsRetryWarningsAndOneSanitisedTerminalError()
    {
        var stub = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(SensitiveResponseBody),
        });
        using var harness = new Harness(stub);

        await harness.ExecuteAsync(NewTestConfig());

        // 1 initial attempt + 3 retries = 4 handler calls, matching the pre-existing retry policy.
        Assert.Equal(4, stub.CallCount);

        var warnings = harness.LogEntries.Where(e => e.Level == LogLevel.Warning).ToArray();
        Assert.Equal(3, warnings.Length);
        for (var i = 0; i < warnings.Length; i++)
        {
            var message = warnings[i].FormattedMessage;
            Assert.Contains($"Request retry {i + 1}", message, StringComparison.Ordinal);
            Assert.Contains(AppName, message, StringComparison.Ordinal);
            Assert.Contains("500", message, StringComparison.Ordinal);
            Assert.Contains("5xx", message, StringComparison.Ordinal);
            AssertSanitised(message);
        }

        var errors = harness.LogEntries.Where(e => e.Level == LogLevel.Error).ToArray();
        // One terminal-cause error inside RunAvailabilityTestAsync + one outer-catch failure log.
        Assert.Equal(2, errors.Length);
        foreach (var error in errors)
        {
            AssertSanitised(error.FormattedMessage);
            Assert.Contains(AppName, error.FormattedMessage, StringComparison.Ordinal);
            if (error.Exception is not null)
            {
                AssertSanitised(error.Exception.Message);
                AssertSanitised(error.Exception.ToString());
            }
        }

        var entry = Assert.Single(harness.Spy.Entries);
        Assert.False(entry.Success);
        Assert.Equal("HTTP 500 (5xx)", entry.Message);
        AssertSanitised(entry.Message);
    }

    [Fact]
    public async Task HttpRequestException_TerminalFailure_ProducesSanitisedAvailabilityFailure()
    {
        var stub = new StubHandler((_, _) => throw new HttpRequestException(
            $"Contrived transport failure for {ExpandedUri}"));
        using var harness = new Harness(stub);

        await harness.ExecuteAsync(NewTestConfig());

        Assert.Equal(4, stub.CallCount);
        var entry = Assert.Single(harness.Spy.Entries);
        Assert.False(entry.Success);
        Assert.Equal("HttpRequestException", entry.Message);

        var warnings = harness.LogEntries.Where(e => e.Level == LogLevel.Warning).ToArray();
        Assert.Equal(3, warnings.Length);
        foreach (var warning in warnings)
        {
            AssertSanitised(warning.FormattedMessage);
            Assert.Contains("HttpRequestException", warning.FormattedMessage, StringComparison.Ordinal);
        }

        var outerError = Assert.Single(harness.LogEntries, e => e.Level == LogLevel.Error);
        AssertSanitised(outerError.FormattedMessage);
        Assert.Contains("HttpRequestException", outerError.FormattedMessage, StringComparison.Ordinal);
        Assert.Contains(AppName, outerError.FormattedMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Timeout_TaskCanceledException_IsRecordedAsSanitisedTimeoutFailure()
    {
        // HttpClient's Timeout expiring surfaces as TaskCanceledException with
        // IsCancellationRequested=false on the caller token; the outer catch must classify it.
        var stub = new StubHandler((_, _) => throw new TaskCanceledException(
            $"Contrived timeout hitting {ExpandedUri}"));
        using var harness = new Harness(stub);

        await harness.ExecuteAsync(NewTestConfig());

        Assert.Equal(4, stub.CallCount);
        var entry = Assert.Single(harness.Spy.Entries);
        Assert.False(entry.Success);
        Assert.Equal("Timeout", entry.Message);

        foreach (var entryLog in harness.LogEntries)
        {
            AssertSanitised(entryLog.FormattedMessage);
        }
    }

    [Fact]
    public async Task Cancellation_ByHostToken_SkipsAvailabilityWrite()
    {
        var stub = new StubHandler((_, ct) => throw new OperationCanceledException(ct));
        using var harness = new Harness(stub);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await harness.ExecuteAsync(NewTestConfig(), cts.Token);

        Assert.Empty(harness.Spy.Entries);
        Assert.DoesNotContain(harness.LogEntries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public void DefaultRetryBackoff_Produces_2_4_8_SecondProgression()
    {
        // Documents the production backoff schedule; the RetryBackoff seam exists for tests only
        // and its default MUST match the previously deployed 2/4/8-second progression.
        var target = new ExternalHealthCheck(
            new ConfigurationBuilder().Build(),
            new StaticOptionsMonitor(new SiteWatchOptions()),
            new StubHttpClientFactory(new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK))),
            new SpyAvailabilityTelemetry(),
            NullLogger<ExternalHealthCheck>.Instance);

        Assert.Equal(TimeSpan.FromSeconds(2), target.RetryBackoff(1));
        Assert.Equal(TimeSpan.FromSeconds(4), target.RetryBackoff(2));
        Assert.Equal(TimeSpan.FromSeconds(8), target.RetryBackoff(3));
    }

    [Fact]
    public void RetryBackoff_Setter_RejectsNull()
    {
        // Guard the internal seam: a null assignment (accidental or via a test refactor) would
        // otherwise defer the failure into Polly's WaitAndRetryAsync at the next timer tick.
        var target = new ExternalHealthCheck(
            new ConfigurationBuilder().Build(),
            new StaticOptionsMonitor(new SiteWatchOptions()),
            new StubHttpClientFactory(new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK))),
            new SpyAvailabilityTelemetry(),
            NullLogger<ExternalHealthCheck>.Instance);

        Assert.Throws<ArgumentNullException>(() => target.RetryBackoff = null!);
    }

    [Fact]
    public void ClassifyFailure_MapsExceptionsToSanitisedSummaries()
    {
        Assert.Equal(
            "HTTP 503 (5xx)",
            ExternalHealthCheck.ClassifyFailure(new HttpRequestException(
                "contains " + ExpandedUri,
                inner: null,
                statusCode: HttpStatusCode.ServiceUnavailable)));
        Assert.Equal(
            "HttpRequestException",
            ExternalHealthCheck.ClassifyFailure(new HttpRequestException("contains " + ExpandedUri)));
        Assert.Equal("Timeout", ExternalHealthCheck.ClassifyFailure(new TaskCanceledException()));
        Assert.Equal("Cancelled", ExternalHealthCheck.ClassifyFailure(new OperationCanceledException()));
        Assert.Equal("InvalidOperationException", ExternalHealthCheck.ClassifyFailure(new InvalidOperationException("x")));
    }

    private static TestConfig NewTestConfig()
    {
        return new TestConfig
        {
            App = AppName,
            AppInsights = "default",
            Uri = ExpandedUri,
            Site = SiteId,
        };
    }

    private static void AssertSanitised(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Assert.DoesNotContain(SecretQueryValue, text, StringComparison.Ordinal);
        Assert.DoesNotContain(SensitiveResponseBody, text, StringComparison.Ordinal);
        Assert.DoesNotContain("?key=", text, StringComparison.Ordinal);
        Assert.DoesNotContain(ExpandedUri, text, StringComparison.Ordinal);
        Assert.DoesNotContain(PathSegment, text, StringComparison.Ordinal);
        // Host-only leakage guard: HttpClient exceptions frequently surface just the hostname in
        // ex.Message (with no path/query). Asserting the bare hostname is absent catches that
        // real-world shape even when the full expanded URI is not present.
        Assert.DoesNotContain(HostName, text, StringComparison.Ordinal);
    }

    private sealed class Harness : IDisposable
    {
        private readonly StubHttpClientFactory factory;

        public Harness(StubHandler handler)
        {
            factory = new StubHttpClientFactory(handler);
            Spy = new SpyAvailabilityTelemetry();
            LoggerProvider = new CollectingLoggerProvider();
            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(LoggerProvider);
            });

            Check = new ExternalHealthCheck(
                new ConfigurationBuilder().Build(),
                new StaticOptionsMonitor(new SiteWatchOptions()),
                factory,
                Spy,
                LoggerFactory.CreateLogger<ExternalHealthCheck>())
            {
                RetryBackoff = _ => TimeSpan.Zero,
            };
        }

        public SpyAvailabilityTelemetry Spy { get; }

        public CollectingLoggerProvider LoggerProvider { get; }

        public ILoggerFactory LoggerFactory { get; }

        public ExternalHealthCheck Check { get; }

        public IReadOnlyList<CapturedLogEntry> LogEntries => LoggerProvider.Entries;

        public async Task ExecuteAsync(TestConfig testConfig, CancellationToken cancellationToken = default)
        {
            // ExecuteTestAsync is private; drive it via the internal RunAvailabilityTestAsync
            // path through the public method by invoking a small internal helper. We reach it
            // through the internal ExecuteTestAsync via reflection to avoid changing the public
            // surface.
            var method = typeof(ExternalHealthCheck).GetMethod(
                "ExecuteTestAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var vt = (ValueTask)method.Invoke(Check, [testConfig, "eastus", cancellationToken])!;
            await vt;
        }

        public void Dispose()
        {
            LoggerFactory.Dispose();
            LoggerProvider.Dispose();
            factory.Dispose();
        }
    }

    internal sealed class SpyAvailabilityTelemetry : IAvailabilityTelemetry
    {
        private readonly List<AvailabilityTelemetryEntry> entries = [];

        public IReadOnlyList<AvailabilityTelemetryEntry> Entries
        {
            get
            {
                lock (entries)
                {
                    return [.. entries];
                }
            }
        }

        public void Track(AvailabilityTelemetryEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            lock (entries)
            {
                entries.Add(entry);
            }
        }
    }

    private sealed class StaticOptionsMonitor(SiteWatchOptions value) : IOptionsMonitor<SiteWatchOptions>
    {
        public SiteWatchOptions CurrentValue { get; } = value;

        public SiteWatchOptions Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<SiteWatchOptions, string?> listener)
        {
            return NullDisposable.Instance;
        }

        private sealed class NullDisposable : IDisposable
        {
            public static NullDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class StubHttpClientFactory(StubHandler handler) : IHttpClientFactory, IDisposable
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(handler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(5),
            };
        }

        public void Dispose()
        {
            handler.Dispose();
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        private int callCount;

        public int CallCount => callCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref callCount);
            try
            {
                var response = respond(request, cancellationToken);
                response.RequestMessage ??= request;
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                return Task.FromException<HttpResponseMessage>(ex);
            }
        }
    }

    internal sealed class CollectingLoggerProvider : ILoggerProvider
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
            var formatted = formatter(state, exception);
            var sb = new StringBuilder(formatted);
            // Include structured state key/value pairs so token/URI leakage via property
            // substitution values (not just the message template) is also asserted.
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    sb.Append('|').Append(pair.Key).Append('=').Append(pair.Value);
                }
            }

            lock (entries)
            {
                entries.Add(new CapturedLogEntry(category, logLevel, eventId, sb.ToString(), exception));
            }
        }
    }

    internal sealed record CapturedLogEntry(
        string Category,
        LogLevel Level,
        EventId EventId,
        string FormattedMessage,
        Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
