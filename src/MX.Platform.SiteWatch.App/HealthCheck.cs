using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MX.Platform.SitewatchFunc;

public class HealthCheck(HealthCheckService healthCheck)
{
    [Function(nameof(HealthCheck))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req,
        FunctionContext context)
    {
        var healthStatus = await healthCheck.CheckHealthAsync();
        return new OkObjectResult(Enum.GetName(healthStatus.Status) ?? "Unknown");
    }
}
