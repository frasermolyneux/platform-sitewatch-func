using MX.Observability.OpenTelemetry.Availability;
using MX.Platform.SiteWatch.App.Availability;

namespace MX.Platform.SiteWatch.App.Tests;

/// <summary>
/// Documents and verifies the explicit telemetry contract dimensions (<c>componentId</c>, <c>siteId</c>,
/// <c>region</c>) that platform-status-web filters/aggregates on. Mirrors the fixture used in
/// platform-status-web's AvailabilityQueryBuilder/ComponentStatusCalculator tests — keep both in sync.
/// </summary>
public sealed class ComponentDimensionTests
{
    [Fact]
    public void Track_WithComponent_IncludesContractDimensionsInProperties()
    {
        var spy = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            spy,
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase));

        var testConfig = new TestConfig
        {
            App = "app-portal-repo-prd",
            AppInsights = "portal",
            Uri = "https://example.invalid/health/live",
            Site = "xi",
            Component = "xi.sitewatch.repository-api-v1"
        };

        var entry = new AvailabilityTelemetryEntry
        {
            Name = testConfig.App,
            Success = true,
            Duration = TimeSpan.FromMilliseconds(42),
            RunLocation = "uksouth",
            Message = "OK",
            Properties = ExternalHealthCheck.BuildContractDimensions(testConfig, "uksouth")
        };

        telemetry.Track(entry);

        Assert.Equal(1, spy.TrackCount);
        Assert.NotNull(spy.LastEntry?.Properties);
        Assert.Equal("xi.sitewatch.repository-api-v1", spy.LastEntry.Properties["componentId"]);
        Assert.Equal("xi", spy.LastEntry.Properties["siteId"]);
        Assert.Equal("uksouth", spy.LastEntry.Properties["region"]);
    }

    [Fact]
    public void Track_WithoutComponent_FallsBackToAppNameForComponentId()
    {
        var spy = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            spy,
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase));

        var testConfig = new TestConfig
        {
            App = "app-portal-repo-prd",
            AppInsights = "portal",
            Uri = "https://example.invalid/health/live",
            Site = "xi",
            Component = null
        };

        var entry = new AvailabilityTelemetryEntry
        {
            Name = testConfig.App,
            Success = true,
            Duration = TimeSpan.FromMilliseconds(42),
            RunLocation = "uksouth",
            Message = "OK",
            Properties = ExternalHealthCheck.BuildContractDimensions(testConfig, "uksouth")
        };

        telemetry.Track(entry);

        Assert.Equal(1, spy.TrackCount);
        Assert.NotNull(spy.LastEntry?.Properties);
        Assert.Equal("app-portal-repo-prd", spy.LastEntry.Properties["componentId"]);
        Assert.Equal("xi", spy.LastEntry.Properties["siteId"]);
    }

    [Fact]
    public void Track_WithEmptyComponent_FallsBackToAppNameForComponentId()
    {
        var spy = new SpyAvailabilityTelemetry();
        using var telemetry = new MultiTargetAvailabilityTelemetry(
            spy,
            new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase));

        var testConfig = new TestConfig
        {
            App = "app-portal-repo-prd",
            AppInsights = "portal",
            Uri = "https://example.invalid/health/live",
            Site = "xi",
            Component = "  "
        };

        var entry = new AvailabilityTelemetryEntry
        {
            Name = testConfig.App,
            Success = true,
            Duration = TimeSpan.FromMilliseconds(42),
            RunLocation = "uksouth",
            Message = "OK",
            Properties = ExternalHealthCheck.BuildContractDimensions(testConfig, "uksouth")
        };

        telemetry.Track(entry);

        Assert.Equal(1, spy.TrackCount);
        Assert.NotNull(spy.LastEntry?.Properties);
        Assert.Equal("app-portal-repo-prd", spy.LastEntry.Properties["componentId"]);
    }

    [Fact]
    public void Track_ContractDimensions_PassedThroughToEmitter()
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

        var testConfig = new TestConfig
        {
            App = "app-portal-web-prd",
            AppInsights = "portal",
            Uri = "https://example.invalid/health/live",
            Site = "xi",
            Component = "xi.sitewatch.portal-web"
        };

        var entry = new AvailabilityTelemetryEntry
        {
            Name = testConfig.App,
            Success = false,
            Duration = TimeSpan.FromMilliseconds(5000),
            RunLocation = "eastus",
            Message = "Timeout",
            Target = "portal",
            Properties = ExternalHealthCheck.BuildContractDimensions(testConfig, "eastus")
        };

        telemetry.Track(entry);

        Assert.Equal(1, portalSpy.TrackCount);
        Assert.NotNull(portalSpy.LastEntry?.Properties);
        Assert.Equal("xi.sitewatch.portal-web", portalSpy.LastEntry.Properties["componentId"]);
        Assert.Equal("xi", portalSpy.LastEntry.Properties["siteId"]);
        Assert.Equal("eastus", portalSpy.LastEntry.Properties["region"]);
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
