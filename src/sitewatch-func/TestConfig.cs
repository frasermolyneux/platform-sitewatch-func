using Newtonsoft.Json;

namespace MX.Platform.SitewatchFunc;

public class TestConfig
{
    [JsonProperty("app")]
    public string App { get; set; }

    [JsonProperty("app_insights")]
    public string AppInsights { get; set; }

    [JsonProperty("uri")]
    public string Uri { get; set; }
}
