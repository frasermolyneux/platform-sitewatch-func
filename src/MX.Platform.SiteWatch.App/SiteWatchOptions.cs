using System.Collections.Generic;

namespace MX.Platform.SiteWatch.App;

public class SiteWatchOptions
{
    /// <summary>
    /// Optional map of named availability-telemetry targets to Application Insights connection strings.
    /// Bound from configuration section <c>SiteWatch:Telemetry</c> and consumed by the SiteWatch-local
    /// <c>MultiTargetAvailabilityTelemetry</c> router so each test's <c>app_insights</c> value can route
    /// to a specific Application Insights resource. Keys are matched case-insensitively.
    /// </summary>
    public Dictionary<string, string> Telemetry { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<TestConfig> Tests { get; set; } = [];

    public bool DisableExternalChecks { get; set; }
}