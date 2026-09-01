using System.Net;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MX.Platform.SitewatchFunc;

public class HealthCheck(HealthCheckService healthCheck)
{
    [Function("HealthCheckReady")]
    public async Task<HttpResponseData> RunReady([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/ready")] HttpRequestData req,
        FunctionContext context)
    {
        var result = await healthCheck.CheckHealthAsync(context.CancellationToken);
        var statusCode = result.Status == HealthStatus.Healthy
            ? HttpStatusCode.OK
            : HttpStatusCode.ServiceUnavailable;

        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new
        {
            status = result.Status.ToString(),
            checks = result.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
            }),
        }, context.CancellationToken);

        return response;
    }

    [Function("HealthCheckLive")]
    public async Task<HttpResponseData> RunLive([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/live")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            status = HealthStatus.Healthy.ToString(),
        });

        return response;
    }
}
