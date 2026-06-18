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
    public void Constructor_WithConnectionString_TracksWithoutThrowing()
    {
        // When a connection string is provided, the default emitter should use a dedicated
        // LoggerFactory that bypasses the host pipeline, guaranteeing 100% sampling.
        using var hostFactory = LoggerFactory.Create(_ => { });
        var targets = new AvailabilityTelemetryTargets();

        // A valid-shaped (but non-functional) connection string to verify construction succeeds.
        const string fakeConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost/";

        using var telemetry = new MultiTargetAvailabilityTelemetry(
            hostFactory, targets, "SiteWatch FuncApp", fakeConnectionString);
        var entry = CreateEntry();

        var exception = Record.Exception(() => telemetry.Track(entry));

        Assert.Null(exception);
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
