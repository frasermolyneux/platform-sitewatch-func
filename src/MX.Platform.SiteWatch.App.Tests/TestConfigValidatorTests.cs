namespace MX.Platform.SiteWatch.App.Tests;

public sealed class TestConfigValidatorTests
{
    [Fact]
    public void DelayedTimerStillCollectsDueChecksWithoutRepeatingWithinTheirInterval()
    {
        var test = new TestConfig
        {
            App = "fixture",
            Site = "mx",
            Component = "mx.fixture",
            AppInsights = "default",
            Uri = "https://example.invalid",
            IntervalSeconds = 300
        };
        var due = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var schedule = new AvailabilitySchedule();
        Assert.True(schedule.TrySchedule(test, due));
        Assert.False(schedule.TrySchedule(test, due.AddSeconds(30)));
        Assert.False(schedule.TrySchedule(test, due.AddSeconds(270)));
        Assert.True(schedule.TrySchedule(test, due.AddSeconds(345)));
        Assert.False(schedule.TrySchedule(test, due.AddSeconds(360)));
        Assert.True(schedule.TrySchedule(test, due.AddSeconds(645)));
        Assert.True(new AvailabilitySchedule().TrySchedule(test, due.AddSeconds(650)));
    }

    [Theory]
    [InlineData(0, "GET", 0)]
    [InlineData(45, "GET", 0)]
    [InlineData(300, "POST", 0)]
    [InlineData(300, "HEAD", 4)]
    public void UnsupportedCollectionBudgetsAreRejected(int interval, string method, int retries)
    {
        var test = new TestConfig
        {
            App = "fixture",
            Site = "mx",
            AppInsights = "default",
            Uri = "https://example.invalid",
            IntervalSeconds = interval,
            Method = method,
            Retries = retries
        };
        Assert.Throws<InvalidOperationException>(() => TestConfigValidator.Validate([test]));
    }

    [Fact]
    public void DefaultChecksRetainEveryTimerTickDespiteSmallInvocationJitter()
    {
        var test = new TestConfig { App = "fixture", Site = "mx", AppInsights = "default", Uri = "https://example.invalid" };
        var schedule = new AvailabilitySchedule();
        var start = DateTimeOffset.UtcNow;
        Assert.True(schedule.TrySchedule(test, start));
        Assert.True(schedule.TrySchedule(test, start.AddSeconds(29.9)));
    }

    [Fact]
    public void DuplicateCheckIdentitiesAreRejectedButDistinctChecksCanShareAComponent()
    {
        var first = new TestConfig
        {
            App = "first",
            Site = "mx",
            Component = "mx.fixture",
            AppInsights = "default",
            Uri = "https://example.invalid"
        };
        var second = new TestConfig
        {
            App = "second",
            Site = "mx",
            Component = "mx.fixture",
            AppInsights = "default",
            Uri = "https://example.invalid"
        };
        Assert.Throws<InvalidOperationException>(() => TestConfigValidator.Validate([first, second]));
        second.CheckId = "mx.fixture.routing";
        TestConfigValidator.Validate([first, second]);
    }

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
