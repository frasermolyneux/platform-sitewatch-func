using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using MX.Observability.OpenTelemetry.Availability;
using MX.Platform.SiteWatch.App.Availability;

namespace MX.Platform.SiteWatch.App.Tests;

public sealed class ApplicationInsightsAvailabilityTelemetryTests
{
    [Fact]
    public void Track_MapsEntryToNativeAvailabilityTelemetry()
    {
        var channel = new CapturingTelemetryChannel();
        using var configuration = new TelemetryConfiguration { TelemetryChannel = channel };
        var client = new TelemetryClient(configuration);
        using var telemetry = new ApplicationInsightsAvailabilityTelemetry(client);
        var timestamp = new DateTimeOffset(2026, 5, 14, 10, 30, 15, TimeSpan.Zero);

        telemetry.Track(new AvailabilityTelemetryEntry
        {
            Id = "custom-id-123",
            Name = "test-app",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(12),
            Timestamp = timestamp,
            RunLocation = "local",
            Message = "OK",
            Properties = new Dictionary<string, string>
            {
                ["component"] = "xi.sitewatch.test-app",
            },
        });

        var captured = Assert.IsType<AvailabilityTelemetry>(Assert.Single(channel.SentItems));
        Assert.Equal("custom-id-123", captured.Id);
        Assert.Equal("test-app", captured.Name);
        Assert.True(captured.Success);
        Assert.Equal(TimeSpan.FromMilliseconds(12), captured.Duration);
        Assert.Equal(timestamp, captured.Timestamp);
        Assert.Equal("local", captured.RunLocation);
        Assert.Equal("OK", captured.Message);
        Assert.Equal("xi.sitewatch.test-app", captured.Properties["component"]);
    }

    private sealed class CapturingTelemetryChannel : ITelemetryChannel
    {
        public List<ITelemetry> SentItems { get; } = [];

        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; } = string.Empty;

        public void Send(ITelemetry item)
        {
            SentItems.Add(item);
        }
        public void Flush() { }
        public void Dispose() { }
    }
}