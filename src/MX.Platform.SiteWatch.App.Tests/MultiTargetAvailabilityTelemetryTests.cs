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
        using var telemetry = new MultiTargetAvailabilityTelemetry(loggerFactory, targets, "Sitewatch FuncApp");
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

    private static AvailabilityTelemetryEntry CreateEntry(string? target = null)
    {
        return new AvailabilityTelemetryEntry
        {
            Name = "test-app",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(12),
            RunLocation = "local",
            Message = "OK",
            Target = target,
        };
    }

    private sealed class SpyAvailabilityTelemetry : IAvailabilityTelemetry
    {
        public int TrackCount { get; private set; }

        public void Track(AvailabilityTelemetryEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            TrackCount++;
        }
    }
}
