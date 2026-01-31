using Newtonsoft.Json;

namespace MX.Platform.SitewatchFunc;

public class TestConfig
{
    [JsonProperty("app")]
    public required string App { get; set; }

    [JsonProperty("app_insights")]
    public required string AppInsights { get; set; }

    [JsonProperty("uri")]
    public required string Uri { get; set; }
}
