namespace MX.Platform.SiteWatch.App.Tests;

public sealed class TestConfigValidatorTests
{
    [Fact]
    public void Validate_WithMissingSite_Throws()
    {
        var tests = new List<TestConfig>
        {
            new() { App = "app", AppInsights = "default", Uri = "https://example.invalid", Site = "" }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => TestConfigValidator.Validate(tests));
        Assert.Contains("site", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithComponentNotMatchingSitePrefix_Throws()
    {
        var tests = new List<TestConfig>
        {
            new() { App = "app", AppInsights = "default", Uri = "https://example.invalid", Site = "xi", Component = "mx.sitewatch.other" }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => TestConfigValidator.Validate(tests));
        Assert.Contains("prefix", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithComponentMatchingSitePrefix_DoesNotThrow()
    {
        var tests = new List<TestConfig>
        {
            new() { App = "app", AppInsights = "default", Uri = "https://example.invalid", Site = "xi", Component = "xi.sitewatch.repository-api" }
        };

        var exception = Record.Exception(() => TestConfigValidator.Validate(tests));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithoutComponent_DoesNotThrow()
    {
        var tests = new List<TestConfig>
        {
            new() { App = "app", AppInsights = "default", Uri = "https://example.invalid", Site = "mx" }
        };

        var exception = Record.Exception(() => TestConfigValidator.Validate(tests));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithEmptyList_DoesNotThrow()
    {
        var exception = Record.Exception(() => TestConfigValidator.Validate([]));
        Assert.Null(exception);
    }
}
