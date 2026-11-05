namespace MX.Platform.SiteWatch.App.Tests;

public sealed class ProductionMonitoringConfigurationTests
{
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
