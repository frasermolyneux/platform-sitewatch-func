using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using MX.Observability.OpenTelemetry.Availability;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace MX.Platform.SiteWatch.App.Availability;

/// <summary>
/// SiteWatch-specific multi-target availability emitter. Each configured target owns a dedicated
/// <see cref="ILoggerFactory"/> wired to its own Azure Monitor log exporter, so a single
/// <see cref="AvailabilityTelemetryEntry"/> can be routed to one of several Application Insights
/// resources based on <see cref="AvailabilityTelemetryEntry.Target"/>. When the target is null,
/// empty, or unknown, the host's default <see cref="ILogger{TCategoryName}"/> is used (which is
/// wired to the host's primary Azure Monitor exporter via the normal OTEL pipeline).
/// <para>
/// This lives in the SiteWatch project rather than in the shared observability NuGet because the
/// "synthetic monitor reports availability into the watched service's Application Insights" pattern
/// is specific to SiteWatch — every other app emits telemetry to its own AI resource.
/// </para>
/// </summary>
internal sealed class MultiTargetAvailabilityTelemetry : IAvailabilityTelemetry, IDisposable
{
    // _targetEmitters and _targetFactories are populated in the constructor and never mutated
    // afterwards (Dispose() clears them only once during shutdown). Track() is therefore safe to
    // call concurrently from multiple threads.
    private readonly IAvailabilityTelemetry defaultEmitter;
    private readonly Dictionary<string, IAvailabilityTelemetry> targetEmitters;
    private readonly List<ILoggerFactory> targetFactories;
    private bool disposed;

    internal MultiTargetAvailabilityTelemetry(
        IAvailabilityTelemetry defaultEmitter,
        IDictionary<string, IAvailabilityTelemetry> targetEmitters)
    {
        this.defaultEmitter = defaultEmitter ?? throw new ArgumentNullException(nameof(defaultEmitter));
        ArgumentNullException.ThrowIfNull(targetEmitters);

        this.targetEmitters = new Dictionary<string, IAvailabilityTelemetry>(targetEmitters, StringComparer.OrdinalIgnoreCase);
        targetFactories = [];
    }

    public MultiTargetAvailabilityTelemetry(
        ILoggerFactory loggerFactory,
        AvailabilityTelemetryTargets targets,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(targets);

        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name must be provided.", nameof(serviceName));

        defaultEmitter = new OpenTelemetryAvailabilityTelemetry(
            loggerFactory.CreateLogger<OpenTelemetryAvailabilityTelemetry>());
        targetEmitters = new Dictionary<string, IAvailabilityTelemetry>(StringComparer.OrdinalIgnoreCase);
        targetFactories = new List<ILoggerFactory>(targets.Targets.Count);

        foreach (var (name, connectionString) in targets.Targets)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Availability target names must be non-empty.", nameof(targets));

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException($"Connection string for availability target '{name}' must be non-empty.", nameof(targets));

            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = false;
                    options.ParseStateValues = false;
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
                    options.AddAzureMonitorLogExporter(o => o.ConnectionString = connectionString);
                });
            });

            targetFactories.Add(factory);
            var logger = factory.CreateLogger<OpenTelemetryAvailabilityTelemetry>();
            targetEmitters[name] = new OpenTelemetryAvailabilityTelemetry(logger);
        }
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
            return;

        // Best-effort flush: disposing each LoggerFactory should drain its Azure Monitor log
        // exporter, but in-flight batches may be lost during rapid host shutdown. Exceptions
        // here are swallowed to avoid masking the app exit; observe missing entries through the
        // host's own Application Insights resource if shutdown delivery becomes a concern.
        foreach (var factory in targetFactories)
        {
            try
            {
                factory.Dispose();
            }
            catch
            {
                // Best-effort flush; swallow exceptions on shutdown to avoid masking app exit.
            }
        }

        targetFactories.Clear();
        targetEmitters.Clear();
        disposed = true;
    }
}
