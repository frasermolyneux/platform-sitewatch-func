using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using MX.Observability.OpenTelemetry.Availability;

namespace MX.Platform.SiteWatch.App.Availability;

/// <summary>
/// SiteWatch-specific multi-target availability emitter. Each configured target owns a dedicated
/// <see cref="TelemetryClient"/> wired to its own Application Insights resource, so a single
/// <see cref="AvailabilityTelemetryEntry"/> can be routed to one of several Application Insights
/// resources based on <see cref="AvailabilityTelemetryEntry.Target"/>. When the target is null,
/// empty, or unknown, the host's default Application Insights resource is used.
/// <para>
/// This lives in the SiteWatch project rather than in the shared observability NuGet because the
/// "synthetic monitor reports availability into the watched service's Application Insights" pattern
/// is specific to SiteWatch — every other app emits telemetry to its own AI resource.
/// </para>
/// </summary>
internal sealed class MultiTargetAvailabilityTelemetry : IAvailabilityTelemetry, IDisposable
{
    // _targetEmitters and _ownedEmitters are populated in the constructor and never mutated
    // afterwards (Dispose() clears them only once during shutdown). Track() is therefore safe to
    // call concurrently from multiple threads.
    private readonly IAvailabilityTelemetry defaultEmitter;
    private readonly Dictionary<string, IAvailabilityTelemetry> targetEmitters;
    private readonly List<ApplicationInsightsAvailabilityTelemetry> ownedEmitters;
    private bool disposed;

    internal MultiTargetAvailabilityTelemetry(
        IAvailabilityTelemetry defaultEmitter,
        IDictionary<string, IAvailabilityTelemetry> targetEmitters)
    {
        this.defaultEmitter = defaultEmitter ?? throw new ArgumentNullException(nameof(defaultEmitter));
        ArgumentNullException.ThrowIfNull(targetEmitters);

        this.targetEmitters = new Dictionary<string, IAvailabilityTelemetry>(targetEmitters, StringComparer.OrdinalIgnoreCase);
        ownedEmitters = [];
    }

    public MultiTargetAvailabilityTelemetry(
        ILoggerFactory loggerFactory,
        AvailabilityTelemetryTargets targets,
        string serviceName)
        : this(loggerFactory, targets, serviceName, connectionString: null)
    {
    }

    public MultiTargetAvailabilityTelemetry(
        ILoggerFactory loggerFactory,
        AvailabilityTelemetryTargets targets,
        string serviceName,
        string? connectionString)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(targets);

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name must be provided.", nameof(serviceName));
        }

        ownedEmitters = new List<ApplicationInsightsAvailabilityTelemetry>(targets.Targets.Count + 1);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var emitter = CreateEmitter(connectionString, serviceName);
            ownedEmitters.Add(emitter);
            defaultEmitter = emitter;
        }
        else
        {
            defaultEmitter = new OpenTelemetryAvailabilityTelemetry(
                loggerFactory.CreateLogger<OpenTelemetryAvailabilityTelemetry>());
        }

        targetEmitters = new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var (name, targetConnectionString) in targets.Targets)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Availability target names must be non-empty.", nameof(targets));
                }

                if (string.IsNullOrWhiteSpace(targetConnectionString))
                {
                    throw new ArgumentException($"Connection string for availability target '{name}' must be non-empty.", nameof(targets));
                }

                var emitter = CreateEmitter(targetConnectionString, serviceName);
                ownedEmitters.Add(emitter);
                targetEmitters[name] = emitter;
            }
        }
        catch
        {
            foreach (var emitter in ownedEmitters)
            {
                try
                {
                    emitter.Dispose();
                }
                catch
                {
                    // Best-effort cleanup: swallow dispose failures to avoid masking the
                    // original constructor exception that triggered this cleanup path.
                }
            }

            throw;
        }
    }

    private static ApplicationInsightsAvailabilityTelemetry CreateEmitter(string connectionString, string serviceName)
    {
        var configuration = TelemetryConfiguration.CreateDefault();
        configuration.ConnectionString = connectionString;

        // Availability checks run every 30 seconds and feed one-minute alert evaluations. Send
        // each result immediately instead of allowing the default channel batch interval to add
        // avoidable alert latency. The production volume is bounded to one item per configured
        // check and region per interval.
        configuration.TelemetryChannel.DeveloperMode = true;

        var client = new TelemetryClient(configuration);
        client.Context.Cloud.RoleName = serviceName;
        return new ApplicationInsightsAvailabilityTelemetry(client, configuration);
    }

    public void Track(AvailabilityTelemetryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!string.IsNullOrWhiteSpace(entry.Target)
            && targetEmitters.TryGetValue(entry.Target, out var targetEmitter))
        {
            targetEmitter.Track(entry);
            return;
        }

        defaultEmitter.Track(entry);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        foreach (var emitter in ownedEmitters)
        {
            try
            {
                emitter.Dispose();
            }
            catch
            {
                // Best-effort flush; swallow exceptions on shutdown to avoid masking app exit.
            }
        }

        ownedEmitters.Clear();
        targetEmitters.Clear();
        disposed = true;
    }
}
