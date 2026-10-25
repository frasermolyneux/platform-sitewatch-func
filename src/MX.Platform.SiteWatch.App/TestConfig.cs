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
    /// Required stable tenant/site identifier for the status page contract (e.g. <c>xi</c>, <c>mx</c>, <c>dev</c>).
    /// Emitted verbatim as the <c>siteId</c> custom dimension on every availability result so consumers
    /// (platform-status-web) can filter by tenant explicitly instead of inferring it from a component-name
    /// prefix at query time.
    /// </summary>
    [JsonPropertyName("site")]
    public required string Site { get; set; }

    /// <summary>
    /// Optional stable component identifier for the status page (e.g. <c>xi.sitewatch.repository-api</c>).
    /// When null or empty, falls back to <see cref="App"/> in the <c>componentId</c> custom dimension.
    /// By convention the first dotted segment MUST equal <see cref="Site"/>; this is validated at startup.
    /// </summary>
    [JsonPropertyName("component")]
    public string? Component { get; set; }

    [JsonPropertyName("check_id")]
    public string? CheckId { get; set; }

    [JsonPropertyName("interval_seconds")]
    public int IntervalSeconds { get; set; } = 30;

    [JsonPropertyName("method")]
    public string Method { get; set; } = "GET";

    [JsonPropertyName("retries")]
    public int Retries { get; set; } = 3;

    [JsonPropertyName("follow_redirects")]
    public bool FollowRedirects { get; set; } = true;

    [JsonPropertyName("headers_only")]
    public bool HeadersOnly { get; set; }

    internal string EffectiveCheckId => CheckId ?? (string.IsNullOrWhiteSpace(Component) ? App : Component);

}
