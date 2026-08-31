using System.Net;
using System.Text.Json;

using Azure.Core.Serialization;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace MX.Platform.SiteWatch.App.Tests;

/// <summary>
/// Covers <see cref="SitewatchFunc.HealthCheck"/> after replacing the removed
/// <c>Microsoft.AspNetCore.Mvc.Core</c>-based <c>IActionResult</c> responses with
/// <see cref="HttpResponseData"/>, exercising the JSON serialisation path directly against the
/// isolated worker HTTP types.
/// </summary>
public sealed class HealthCheckTests
{
    [Fact]
    public async Task RunLive_ReturnsOk_WithHealthyStatus()
    {
        var sut = new SitewatchFunc.HealthCheck(new StubHealthCheckService(HealthStatus.Healthy));
        var request = new FakeHttpRequestData();

        var response = await sut.RunLive(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = ReadJson(response);
        Assert.Equal("Healthy", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task RunReady_ReturnsOk_WhenHealthy()
    {
        var sut = new SitewatchFunc.HealthCheck(new StubHealthCheckService(HealthStatus.Healthy));
        var request = new FakeHttpRequestData();

        var response = await sut.RunReady(request, request.FunctionContext);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = ReadJson(response);
        Assert.Equal("Healthy", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task RunReady_ReturnsServiceUnavailable_WhenUnhealthy()
    {
        var sut = new SitewatchFunc.HealthCheck(new StubHealthCheckService(HealthStatus.Unhealthy));
        var request = new FakeHttpRequestData();

        var response = await sut.RunReady(request, request.FunctionContext);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var payload = ReadJson(response);
        Assert.Equal("Unhealthy", payload.GetProperty("status").GetString());
    }

    private static JsonElement ReadJson(HttpResponseData response)
    {
        response.Body.Position = 0;
        return JsonDocument.Parse(response.Body).RootElement.Clone();
    }

    private sealed class StubHealthCheckService(HealthStatus status) : HealthCheckService
    {
        public override Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool>? predicate, CancellationToken cancellationToken = default)
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                ["stub"] = new HealthReportEntry(status, "stub check", TimeSpan.Zero, exception: null, data: null),
            };

            return Task.FromResult(new HealthReport(entries, TimeSpan.Zero));
        }
    }

    private sealed class FakeFunctionContext : FunctionContext
    {
        public override string InvocationId { get; } = Guid.NewGuid().ToString();

        public override string FunctionId { get; } = "HealthCheck";

        public override TraceContext TraceContext => throw new NotSupportedException();

        public override BindingContext BindingContext => throw new NotSupportedException();

        public override RetryContext RetryContext => throw new NotSupportedException();

        public override IServiceProvider InstanceServices { get; set; } = new EmptyServiceProvider();

        public override FunctionDefinition FunctionDefinition => throw new NotSupportedException();

        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

        public override IInvocationFeatures Features => throw new NotSupportedException();

        private sealed class EmptyServiceProvider : IServiceProvider
        {
            private readonly IOptions<WorkerOptions> workerOptions = Options.Create(new WorkerOptions
            {
                Serializer = new JsonObjectSerializer(),
            });

            public object? GetService(Type serviceType)
            {
                return serviceType == typeof(IOptions<WorkerOptions>) ? workerOptions : null;
            }
        }
    }

    private sealed class FakeHttpRequestData : HttpRequestData
    {
        public FakeHttpRequestData()
            : base(new FakeFunctionContext())
        {
            FunctionContext = (FakeFunctionContext)base.FunctionContext;
        }

        public new FakeFunctionContext FunctionContext { get; }

        public override Stream Body { get; } = new MemoryStream();

        public override HttpHeadersCollection Headers { get; } = [];

        public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = [];

        public override Uri Url { get; } = new("http://localhost/api/health/ready");

        public override IEnumerable<System.Security.Claims.ClaimsIdentity> Identities { get; } = [];

        public override string Method { get; } = "GET";

        public override HttpResponseData CreateResponse()
        {
            return new FakeHttpResponseData(FunctionContext);
        }
    }

    private sealed class FakeHttpResponseData(FunctionContext functionContext) : HttpResponseData(functionContext)
    {
        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers { get; set; } = [];

        public override Stream Body { get; set; } = new MemoryStream();

        public override HttpCookies Cookies => throw new NotSupportedException();
    }
}
