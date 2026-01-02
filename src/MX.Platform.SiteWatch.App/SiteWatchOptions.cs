using System.Collections.Generic;

namespace MX.Platform.SitewatchFunc;

public class SiteWatchOptions
{
    public Dictionary<string, string> Telemetry { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<TestConfig> Tests { get; set; } = new();

    public bool DisableExternalChecks { get; set; }
}