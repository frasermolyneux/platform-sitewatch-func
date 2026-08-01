using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MX.Observability.OpenTelemetry.Filtering;
using MX.Observability.OpenTelemetry.Filtering.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace MX.Platform.SiteWatch.App.Tests;

public sealed class SiteWatchHttpClientTracingTests
{
    [Fact]
    public async Task LoggingFilter_PreservesSuccessfulAndFailedHttpActivities()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var serveRequests = ServeRequests(listener, expectedRequests: 2);
        var exporter = new CapturingActivityExporter();
        var filterOptions = new TelemetryFilterOptions
        {
            Dependencies = new DependencyFilterOptions
            {
                Enabled = true,
                DurationThresholdMs = 0,
            },
        };
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddHttpClientInstrumentation()
            .AddProcessor(new TracingFilterProcessor(
                new StaticOptionsMonitor<TelemetryFilterOptions>(filterOptions),
                NullLogger<TracingFilterProcessor>.Instance))
            .AddProcessor(new SimpleActivityExportProcessor(exporter))
            .Build();
        var services = new ServiceCollection();
        services.AddLogging(SiteWatchHttpClient.ConfigureLogging);
        services.AddHttpClient(SiteWatchHttpClient.Name);
        await using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(SiteWatchHttpClient.Name);

        using var successResponse = await client.GetAsync($"http://127.0.0.1:{port}/success");
        using var failureResponse = await client.GetAsync($"http://127.0.0.1:{port}/failure");
        await serveRequests;
        listener.Stop();
        tracerProvider.ForceFlush();

        Assert.Equal(HttpStatusCode.OK, successResponse.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failureResponse.StatusCode);
        var expectedPort = port.ToString(CultureInfo.InvariantCulture);
        var httpActivities = exporter.Activities
            .Where(activity =>
                activity.Kind == ActivityKind.Client &&
                activity.ServerPort == expectedPort &&
                activity.StatusCode is not null)
            .ToArray();
        Assert.Collection(
            httpActivities,
            activity => AssertActivity(activity, HttpStatusCode.OK),
            activity => AssertActivity(activity, HttpStatusCode.ServiceUnavailable));
    }

    private static void AssertActivity(CapturedActivity activity, HttpStatusCode expectedStatusCode)
    {
        Assert.Equal(((int)expectedStatusCode).ToString(CultureInfo.InvariantCulture), activity.StatusCode);
        Assert.True(activity.Duration > TimeSpan.Zero);
    }

    private static async Task ServeRequests(TcpListener listener, int expectedRequests)
    {
        for (var requestIndex = 0; requestIndex < expectedRequests; requestIndex++)
        {
            using var client = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(10));
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            var requestLine = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10))))
            {
            }

            var statusCode = requestLine?.Contains(" /success ", StringComparison.Ordinal) == true
                ? HttpStatusCode.OK
                : HttpStatusCode.ServiceUnavailable;
            var response = $"HTTP/1.1 {(int)statusCode} {statusCode}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
        }
    }

    private sealed class CapturingActivityExporter : BaseExporter<Activity>
    {
        private readonly List<CapturedActivity> activities = [];

        public IReadOnlyList<CapturedActivity> Activities
        {
            get
            {
                lock (activities)
                {
                    return [.. activities];
                }
            }
        }

        public override ExportResult Export(in Batch<Activity> batch)
        {
            lock (activities)
            {
                foreach (var activity in batch)
                {
                    activities.Add(new CapturedActivity(
                        activity.Kind,
                        activity.GetTagItem("server.port")?.ToString()
                            ?? activity.GetTagItem("net.peer.port")?.ToString(),
                        activity.GetTagItem("http.response.status_code")?.ToString()
                            ?? activity.GetTagItem("http.status_code")?.ToString(),
                        activity.Duration));
                }
            }

            return ExportResult.Success;
        }
    }

    private sealed class StaticOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue => currentValue;

        public TOptions Get(string? name)
        {
            return currentValue;
        }

        public IDisposable? OnChange(Action<TOptions, string?> listener)
        {
            return null;
        }
    }

    private sealed record CapturedActivity(
        ActivityKind Kind,
        string? ServerPort,
        string? StatusCode,
        TimeSpan Duration);
}
