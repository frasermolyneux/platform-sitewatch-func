using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MX.Observability.OpenTelemetry.Availability;
using MX.Platform.SiteWatch.App.Availability;

namespace MX.Platform.SiteWatch.App.Tests;

public sealed class MultiTargetAvailabilityTelemetryTests
{
    [Fact]
    public void Constructor_WithLoggerFactory_TracksDefaultEntryWithoutThrowing()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var targets = new AvailabilityTelemetryTargets();
        using var telemetry = new MultiTargetAvailabilityTelemetry(loggerFactory, targets, "SiteWatch FuncApp");
        var entry = CreateEntry();

        var exception = Record.Exception(() => telemetry.Track(entry));

        Assert.Null(exception);
    }

    [Fact]
    public void Track_WithKnownTarget_UsesTargetEmitter()
    {
        var defaultEmitter = new SpyAvailabilityTelemetry();
        var targetEmitter = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            defaultEmitter,
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase)
            {
                ["target-a"] = targetEmitter,
            });
        var entry = CreateEntry(target: "TARGET-A");

        telemetry.Track(entry);

        Assert.Equal(0, defaultEmitter.TrackCount);
        Assert.Equal(1, targetEmitter.TrackCount);
    }

    [Fact]
    public void Track_WithUnknownTarget_UsesDefaultEmitter()
    {
        var defaultEmitter = new SpyAvailabilityTelemetry();
        var targetEmitter = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            defaultEmitter,
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase)
            {
                ["target-a"] = targetEmitter,
            });
        var entry = CreateEntry(target: "target-b");

        telemetry.Track(entry);

        Assert.Equal(1, defaultEmitter.TrackCount);
        Assert.Equal(0, targetEmitter.TrackCount);
    }

    [Fact]
    public void Track_WithNullTarget_UsesDefaultEmitter()
    {
        var defaultEmitter = new SpyAvailabilityTelemetry();
        var targetEmitter = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            defaultEmitter,
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase)
            {
                ["target-a"] = targetEmitter,
            });
        var entry = CreateEntry(target: null);

        telemetry.Track(entry);

        Assert.Equal(1, defaultEmitter.TrackCount);
        Assert.Equal(0, targetEmitter.TrackCount);
    }

    [Fact]
    public async Task Constructor_WithConnectionString_ExportsNativeAvailabilityData()
    {
        using var listener = new HttpListener();
        var port = GetAvailablePort();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        using var hostFactory = LoggerFactory.Create(_ => { });
        var targets = new AvailabilityTelemetryTargets();
        var connectionString = $"InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:{port}/";
        var telemetry = new MultiTargetAvailabilityTelemetry(
            hostFactory, targets, "SiteWatch FuncApp", connectionString);
        var timestamp = new DateTimeOffset(2026, 5, 14, 10, 30, 15, TimeSpan.Zero);
        var entry = new AvailabilityTelemetryEntry
        {
            Id = "custom-id-123",
            Name = "test-app",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(12),
            Timestamp = timestamp,
            RunLocation = "local",
            Message = "OK",
            Properties = new Dictionary<string, string>
            {
                ["sitewatch.app"] = "portal-web",
                ["sitewatch.environment"] = "prd",
            },
        };

        telemetry.Track(entry);
        telemetry.Dispose();

        var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(15));
        var requestBody = await ReadRequestBody(context.Request);
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.Close();

        using var document = JsonDocument.Parse(requestBody);
        var envelope = document.RootElement;
        Assert.Equal(JsonValueKind.Object, envelope.ValueKind);
        var data = envelope.GetProperty("data");
        var baseData = data.GetProperty("baseData");

        Assert.Equal(timestamp, envelope.GetProperty("time").GetDateTimeOffset());
        Assert.Equal("AvailabilityData", data.GetProperty("baseType").GetString());
        Assert.Equal("custom-id-123", baseData.GetProperty("id").GetString());
        Assert.Equal("test-app", baseData.GetProperty("name").GetString());
        Assert.Equal("00:00:00.0120000", baseData.GetProperty("duration").GetString());
        Assert.True(baseData.GetProperty("success").GetBoolean());
        Assert.Equal("local", baseData.GetProperty("runLocation").GetString());
        Assert.Equal("OK", baseData.GetProperty("message").GetString());

        var properties = baseData.GetProperty("properties");
        Assert.Equal("portal-web", properties.GetProperty("sitewatch.app").GetString());
        Assert.Equal("prd", properties.GetProperty("sitewatch.environment").GetString());
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task<string> ReadRequestBody(HttpListenerRequest request)
    {
        Stream stream = request.InputStream;
        if (string.Equals(request.Headers["Content-Encoding"], "gzip", StringComparison.OrdinalIgnoreCase))
        {
            stream = new GZipStream(stream, CompressionMode.Decompress);
        }

        using (stream)
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            return await reader.ReadToEndAsync();
        }
    }

    [Fact]
    public void Constructor_WithNullConnectionString_TracksWithoutThrowing()
    {
        using var hostFactory = LoggerFactory.Create(_ => { });
        var targets = new AvailabilityTelemetryTargets();

        using var telemetry = new MultiTargetAvailabilityTelemetry(
            hostFactory, targets, "SiteWatch FuncApp", connectionString: null);
        var entry = CreateEntry();

        var exception = Record.Exception(() => telemetry.Track(entry));

        Assert.Null(exception);
    }

    private static AvailabilityTelemetryEntry CreateEntry(string? target = null)
    {
        return CreateEntry(target, properties: null);
    }

    private static AvailabilityTelemetryEntry CreateEntry(string? target, IReadOnlyDictionary<string, string>? properties)
    {
        return new AvailabilityTelemetryEntry
        {
            Name = "test-app",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(12),
            RunLocation = "local",
            Message = "OK",
            Target = target,
            Properties = properties,
        };
    }

    private sealed class SpyAvailabilityTelemetry : IAvailabilityTelemetry
    {
        public int TrackCount { get; private set; }
        public AvailabilityTelemetryEntry? LastEntry { get; private set; }

        public void Track(AvailabilityTelemetryEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            TrackCount++;
            LastEntry = entry;
        }
    }
}
