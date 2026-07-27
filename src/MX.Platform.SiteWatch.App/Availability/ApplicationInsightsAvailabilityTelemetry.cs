using System.Diagnostics;
using System.Globalization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using MX.Observability.OpenTelemetry.Availability;

namespace MX.Platform.SiteWatch.App.Availability;

internal sealed class ApplicationInsightsAvailabilityTelemetry(
    TelemetryClient telemetryClient,
    TelemetryConfiguration? ownedConfiguration = null) : IAvailabilityTelemetry, IDisposable
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(5);
    private readonly TelemetryClient telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    private readonly TelemetryConfiguration? ownedConfiguration = ownedConfiguration;

    public void Track(AvailabilityTelemetryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            throw new ArgumentException("Availability name must be provided.", nameof(entry));
        }

        if (entry.Duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(entry), "Availability duration cannot be negative.");
        }

        var telemetry = new AvailabilityTelemetry
        {
            Id = GetAvailabilityId(entry),
            Name = entry.Name,
            Success = entry.Success,
            Duration = entry.Duration,
            Timestamp = entry.Timestamp,
            RunLocation = entry.RunLocation,
            Message = entry.Message,
        };

        if (entry.Properties is not null)
        {
            foreach (var (key, value) in entry.Properties)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException("Availability property keys must be non-empty.", nameof(entry));
                }

                telemetry.Properties[key] = value;
            }
        }

        telemetryClient.TrackAvailability(telemetry);
    }

    public void Dispose()
    {
        using var cancellationSource = new CancellationTokenSource(FlushTimeout);

        try
        {
            telemetryClient.FlushAsync(cancellationSource.Token).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        finally
        {
            ownedConfiguration?.Dispose();
        }
    }

    private static string GetAvailabilityId(AvailabilityTelemetryEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Id))
        {
            return entry.Id;
        }

        var spanId = Activity.Current?.SpanId.ToString();
        return !string.IsNullOrWhiteSpace(spanId)
            ? spanId
            : Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
    }
}