namespace MX.Platform.SiteWatch.App.Tests;

public sealed class ProductionMonitoringConfigurationTests
{
    [Fact]
    public void Forums_RetainsWebMonitoringWithoutTaskTrigger()
    {
        var configuration = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "terraform", "tfvars", "prd.tfvars"));
        var bindings = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "contract", "status-monitoring-bindings.json"));

        Assert.Contains("xi.sitewatch.forums-web", configuration, StringComparison.Ordinal);
        Assert.Contains("xi.sitewatch.forums-web", bindings, StringComparison.Ordinal);
        Assert.DoesNotContain("xi.sitewatch.forums-tasks", configuration, StringComparison.Ordinal);
        Assert.DoesNotContain("www.xtremeidiots.com-tasks", configuration, StringComparison.Ordinal);
        Assert.DoesNotContain("xtremeidiots_forums_task_key", configuration, StringComparison.Ordinal);
        Assert.DoesNotContain("xi.sitewatch.forums-tasks", bindings, StringComparison.Ordinal);
    }

    [Fact]
    public void TalkWithTiles_UsesCanonicalUrlAndPreservesTelemetryIdentity()
    {
        var configuration = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "terraform", "tfvars", "prd.tfvars"));
        var blockStart = configuration.IndexOf(
            "workload         = \"talkwithtiles\"",
            StringComparison.Ordinal);

        Assert.True(blockStart >= 0, "The production Talk With Tiles probe should be configured.");

        var blockEnd = configuration.IndexOf("\n  },", blockStart, StringComparison.Ordinal);
        Assert.True(blockEnd >= 0, "The production Talk With Tiles probe block should be complete.");

        var talkWithTiles = configuration[blockStart..blockEnd];

        Assert.Contains("uri              = \"https://talkwithtiles.com/\"", talkWithTiles, StringComparison.Ordinal);
        Assert.Contains("app              = \"mx.probe.talkwithtiles\"", talkWithTiles, StringComparison.Ordinal);
        Assert.Contains("component        = \"mx.probe.talkwithtiles\"", talkWithTiles, StringComparison.Ordinal);
        Assert.Contains("app_insights     = \"platform\"", talkWithTiles, StringComparison.Ordinal);
        Assert.Contains("interval_seconds = 300", talkWithTiles, StringComparison.Ordinal);
        Assert.DoesNotContain("talkwithtiles.molyneux.me", configuration, StringComparison.Ordinal);
    }
}
