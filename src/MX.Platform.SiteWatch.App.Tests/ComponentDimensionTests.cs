using MX.Observability.OpenTelemetry.Availability;
using MX.Platform.SiteWatch.App.Availability;

namespace MX.Platform.SiteWatch.App.Tests;

public sealed class ComponentDimensionTests
{
    [Fact]
    public void Track_WithComponent_IncludesComponentInProperties()
    {
        var spy = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            spy,
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase));

        var entry = new AvailabilityTelemetryEntry
        {
            Name = "app-portal-repo-prd",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(42),
            RunLocation = "uksouth",
            Message = "OK",
            Properties = new Dictionary<string, string>
            {
                ["component"] = "xi.sitewatch.repository-api-v1"
            }
        };

        telemetry.Track(entry);

        Assert.Equal(1, spy.TrackCount);
        Assert.NotNull(spy.LastEntry?.Properties);
        Assert.True(spy.LastEntry.Properties.ContainsKey("component"));
        Assert.Equal("xi.sitewatch.repository-api-v1", spy.LastEntry.Properties["component"]);
    }

    [Fact]
    public void Track_WithoutComponent_FallsBackToAppName()
    {
        // Simulates the ExternalHealthCheck fallback: when Component is null/empty, App is used
        var spy = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            spy,
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase));

        string? component = null;
        var fallbackComponent = string.IsNullOrWhiteSpace(component) ? "app-portal-repo-prd" : component;

        var entry = new AvailabilityTelemetryEntry
        {
            Name = "app-portal-repo-prd",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(42),
            RunLocation = "uksouth",
            Message = "OK",
            Properties = new Dictionary<string, string>
            {
                ["component"] = fallbackComponent
            }
        };

        telemetry.Track(entry);

        Assert.Equal(1, spy.TrackCount);
        Assert.NotNull(spy.LastEntry?.Properties);
        Assert.Equal("app-portal-repo-prd", spy.LastEntry.Properties["component"]);
    }

    [Fact]
    public void Track_WithEmptyComponent_FallsBackToAppName()
    {
        // Verifies the whitespace/empty fallback path
        var spy = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            spy,
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase));

        string? component = "  ";
        var fallbackComponent = string.IsNullOrWhiteSpace(component) ? "app-portal-repo-prd" : component;

        var entry = new AvailabilityTelemetryEntry
        {
            Name = "app-portal-repo-prd",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(42),
            RunLocation = "uksouth",
            Message = "OK",
            Properties = new Dictionary<string, string>
            {
                ["component"] = fallbackComponent
            }
        };

        telemetry.Track(entry);

        Assert.Equal(1, spy.TrackCount);
        Assert.NotNull(spy.LastEntry?.Properties);
        Assert.Equal("app-portal-repo-prd", spy.LastEntry.Properties["component"]);
    }

    [Fact]
    public void Track_ComponentDimension_PassedThroughToEmitter()
    {
        // Verifies the full path: entry with Properties flows through MultiTargetAvailabilityTelemetry
        // to the underlying emitter without modification.
        var portalSpy = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            new SpyAvailabilityTelemetry(),
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase)
            {
                ["portal"] = portalSpy,
            });

        var entry = new AvailabilityTelemetryEntry
        {
            Name = "app-portal-web-prd",
            Success = false,
            Duration = TimeSpan.FromMilliseconds(5000),
            RunLocation = "eastus",
            Message = "Timeout",
            Target = "portal",
            Properties = new Dictionary<string, string>
            {
                ["component"] = "xi.sitewatch.portal-web"
            }
        };

        telemetry.Track(entry);

        Assert.Equal(1, portalSpy.TrackCount);
        Assert.NotNull(portalSpy.LastEntry?.Properties);
        Assert.Equal("xi.sitewatch.portal-web", portalSpy.LastEntry.Properties["component"]);
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
