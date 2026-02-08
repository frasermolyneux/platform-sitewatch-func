using System.Text.Json.Serialization;

namespace MX.Platform.SiteWatch.App;

public class TestConfig
{
    [JsonPropertyName("app")]
    public required string App { get; set; }

    [JsonPropertyName("app_insights")]
    public required string AppInsights { get; set; }

    [JsonPropertyName("uri")]
    public required string Uri { get; set; }
}
