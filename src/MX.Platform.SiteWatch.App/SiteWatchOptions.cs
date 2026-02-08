using System.Collections.Generic;

namespace MX.Platform.SiteWatch.App;

public class SiteWatchOptions
{
    public Dictionary<string, string> Telemetry { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<TestConfig> Tests { get; set; } = [];

    public bool DisableExternalChecks { get; set; }
}