# SiteWatch telemetry

SiteWatch has separate Azure Functions host and .NET isolated worker telemetry pipelines:

- The Functions host emits invocation lifecycle traces, request records, exceptions, and runtime
  warnings.
- The isolated worker emits application logs, OpenTelemetry spans, and native Application Insights
  availability results configured in `Program.cs`.

The worker pipeline does not receive host-originated telemetry, so host volume is reduced
deterministically in `host.json`:

```json
"logLevel": {
  "Function": "Warning",
  "Function.HealthCheck": "Warning",
  "Host.Results": "Error"
}
```

`Function = Warning` suppresses successful function start and completion traces emitted at
`Information`, while retaining warnings, errors, and exceptions. The existing
`Function.HealthCheck = Warning` policy remains explicit for the health endpoint.

`Host.Results = Error` suppresses successful host invocation request records while retaining failed
function execution requests. Host Application Insights sampling is disabled so retention is based
on severity rather than probabilistic sampling.

The worker configuration in `Program.cs`, availability telemetry routing, processing behavior,
retries, and health checks are unchanged. Terraform availability alerts use the native
`availabilityResults/availabilityPercentage` metric and do not depend on successful host request
records.

References:

- [Configure Azure Functions monitoring categories and log levels](https://learn.microsoft.com/azure/azure-functions/configure-monitoring#configure-categories)
- [Azure Functions host.json reference](https://learn.microsoft.com/azure/azure-functions/functions-host-json#applicationinsights)
- [.NET isolated worker Application Insights](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide#application-insights)
