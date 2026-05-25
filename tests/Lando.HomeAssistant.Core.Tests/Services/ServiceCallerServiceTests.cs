using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Exceptions;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq.Protected;

namespace Lando.HomeAssistant.Services.Tests;

/// <summary>
/// <see cref="ServiceCallerService"/> turns a typed
/// <see cref="HomeAssistantRequest"/> into an HA REST service call. Tests
/// pin the URL shape (<c>services/{domain}/{service}</c>), the JSON body, and
/// failure translation into <see cref="HomeAssistantException"/>.
/// </summary>
public class ServiceCallerServiceTests
{
    [Fact]
    public async Task Posts_service_call_with_entity_id_in_body()
    {
        var captured = new List<(HttpRequestMessage Request, string Body)>();
        var sut = BuildSut((req, ct) =>
        {
            var body = req.Content!.ReadAsStringAsync(ct).Result;
            captured.Add((req, body));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await sut.CallServiceAsync(HomeAssistantRequest.TurnOn("light.kitchen", brightness: 80), CancellationToken.None);

        captured[0].Request.Method.ShouldBe(HttpMethod.Post);
        captured[0].Request.RequestUri!.AbsolutePath.ShouldEndWith("services/light/turn_on");
        captured[0].Body.ShouldContain("light.kitchen");
        captured[0].Body.ShouldContain("brightness_pct");
    }

    [Fact]
    public async Task Throws_HomeAssistantException_on_non_success_status()
    {
        var sut = BuildSut((_, _) => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream broke"),
        });

        await Should.ThrowAsync<HomeAssistantException>(
            () => sut.CallServiceAsync(HomeAssistantRequest.TurnOff("light.kitchen"), CancellationToken.None));
    }

    [Fact]
    public async Task Wraps_transport_exception_as_HomeAssistantException()
    {
        var inner = new HttpRequestException("connection reset");
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(inner);
        var sut = Build(handler.Object);

        var ex = await Should.ThrowAsync<HomeAssistantException>(
            () => sut.CallServiceAsync(HomeAssistantRequest.TurnOff("light.kitchen"), CancellationToken.None));
        ex.InnerException.ShouldBeSameAs(inner);
    }

    private static ServiceCallerService BuildSut(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, ct) => Task.FromResult(respond(req, ct)));
        return Build(handler.Object);
    }

    private static ServiceCallerService Build(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://homeassistant.example.local:8123/api/") };
        return new ServiceCallerService(http, Options.Create(new JsonSerializerOptions()),
            NullLogger<ServiceCallerService>.Instance);
    }
}
