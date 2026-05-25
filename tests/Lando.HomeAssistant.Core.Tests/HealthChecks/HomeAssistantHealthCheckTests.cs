using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lando.HomeAssistant.Core.HealthChecks.Tests;

public class HomeAssistantHealthCheckTests
{
    private const string BaseUrl = "http://ha.local:8123/api/";

    private static HttpClient MakeClient(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond,
        string? baseAddress = BaseUrl)
    {
        var client = new HttpClient(new FakeHandler(respond));
        if (baseAddress != null)
            client.BaseAddress = new Uri(baseAddress);
        return client;
    }

    private static Mock<IKeyedServiceProvider> BuildProvider(HttpClient client)
    {
        var provider = new Mock<IKeyedServiceProvider>();
        provider.Setup(p => p.GetKeyedService(typeof(HttpClient), Constants.HomeAssistant))
            .Returns(client);
        provider.Setup(p => p.GetRequiredKeyedService(typeof(HttpClient), Constants.HomeAssistant))
            .Returns(client);
        return provider;
    }

    [Fact]
    public async Task CheckHealthAsync_SuccessResponse_ReturnsHealthy()
    {
        using var client = MakeClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var check = new HomeAssistantHealthCheck(BuildProvider(client).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("Home Assistant is reachable");
        result.Data["host"].ShouldBe(BaseUrl);
    }

    [Fact]
    public async Task CheckHealthAsync_NullBaseAddress_HostReportedAsUndefined()
    {
        using var client = MakeClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK), baseAddress: null);
        var check = new HomeAssistantHealthCheck(BuildProvider(client).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // GetAsync("") throws with no BaseAddress; data["host"] is still populated before the throw
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data["host"].ShouldBe("UNDEFINED");
    }

    [Fact]
    public async Task CheckHealthAsync_NonSuccessStatusCode_ReturnsUnhealthy()
    {
        // EnsureSuccessStatusCode() throws HttpRequestException for 4xx/5xx
        using var client = MakeClient((_, _) => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var check = new HomeAssistantHealthCheck(BuildProvider(client).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Cannot reach Home Assistant");
        result.Exception.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task CheckHealthAsync_HttpRequestException_ReturnsUnhealthyWithException()
    {
        var ex = new HttpRequestException("connection refused");
        using var client = MakeClient((_, _) => throw ex);
        var check = new HomeAssistantHealthCheck(BuildProvider(client).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Cannot reach Home Assistant");
        result.Exception.ShouldBeSameAs(ex);
    }

    [Fact]
    public async Task CheckHealthAsync_TaskCanceledException_ReturnsUnhealthyTimeout()
    {
        var ex = new TaskCanceledException("request timed out");
        using var client = MakeClient((_, _) => throw ex);
        var check = new HomeAssistantHealthCheck(BuildProvider(client).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Home Assistant request timed out");
        result.Exception.ShouldBeSameAs(ex);
    }

    [Fact]
    public async Task CheckHealthAsync_UnexpectedException_ReturnsUnhealthyUnexpected()
    {
        var ex = new InvalidOperationException("something went wrong");
        using var client = MakeClient((_, _) => throw ex);
        var check = new HomeAssistantHealthCheck(BuildProvider(client).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Unexpected error checking Home Assistant health");
        result.Exception.ShouldBeSameAs(ex);
    }

    [Fact]
    public async Task CheckHealthAsync_DataContainsHost_OnFailurePaths()
    {
        var ex = new HttpRequestException("refused");
        using var client = MakeClient((_, _) => throw ex);
        var check = new HomeAssistantHealthCheck(BuildProvider(client).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Data.ContainsKey("host").ShouldBeTrue();
        result.Data["host"].ShouldBe(BaseUrl);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(respond(request, cancellationToken));
            }
            catch (Exception ex)
            {
                return Task.FromException<HttpResponseMessage>(ex);
            }
        }
    }
}
