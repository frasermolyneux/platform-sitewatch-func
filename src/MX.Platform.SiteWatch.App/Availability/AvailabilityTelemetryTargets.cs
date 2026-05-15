namespace MX.Platform.SiteWatch.App.Availability;

/// <summary>
/// Configuration for multi-target availability telemetry emission.
/// Each entry maps a target name (referenced by <c>AvailabilityTelemetryEntry.Target</c>) to the
/// Application Insights connection string that should receive entries for that target.
/// </summary>
internal sealed class AvailabilityTelemetryTargets
{
    /// <summary>
    /// Map of target name to Application Insights connection string. Target names are matched
    /// case-insensitively against <c>AvailabilityTelemetryEntry.Target</c>.
    /// </summary>
    public IDictionary<string, string> Targets { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
