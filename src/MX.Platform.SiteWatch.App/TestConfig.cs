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

    /// <summary>
    /// Optional stable component identifier for the status page (e.g. <c>xi.sitewatch.repository-api</c>).
    /// When null or empty, falls back to <see cref="App"/> in the <c>component</c> custom dimension.
    /// </summary>
    [JsonPropertyName("component")]
    public string? Component { get; set; }
}
