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
/// failure translation into <see cref="HomeAssistantException"/>, plus the
/// script-as-a-service call with <c>return_response</c>.
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

    [Fact]
    public async Task RunScriptWithResponse_posts_to_the_script_service_with_return_response()
    {
        var captured = new List<(HttpRequestMessage Request, string Body)>();
        var sut = BuildSut((req, ct) =>
        {
            captured.Add((req, req.Content!.ReadAsStringAsync(ct).Result));
            return Json(HttpStatusCode.OK, """{"changed_states":[],"service_response":{"speech":"Okay, home mode.","allowed":true}}""");
        });

        var result = await sut.RunScriptWithResponseAsync("script.lando_set_home_mode",
            new Dictionary<string, object?> { ["mode"] = "home" }, CancellationToken.None);

        captured[0].Request.Method.ShouldBe(HttpMethod.Post);
        captured[0].Request.RequestUri!.AbsolutePath.ShouldEndWith("services/script/lando_set_home_mode");
        captured[0].Request.RequestUri!.Query.ShouldContain("return_response");
        captured[0].Body.ShouldBe("""{"mode":"home"}""");
        captured[0].Body.ShouldNotContain("entity_id");
        result.ShouldNotBeNull();
        result.Value.GetProperty("speech").GetString().ShouldBe("Okay, home mode.");
        result.Value.GetProperty("allowed").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task RunScriptWithResponse_sends_an_empty_object_without_variables()
    {
        string? body = null;
        var sut = BuildSut((req, ct) =>
        {
            body = req.Content!.ReadAsStringAsync(ct).Result;
            return Json(HttpStatusCode.OK, """{"changed_states":[],"service_response":{}}""");
        });

        var result = await sut.RunScriptWithResponseAsync("script.example_routine", null, CancellationToken.None);

        body.ShouldBe("{}");
        result!.Value.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public async Task RunScriptWithResponse_returns_null_when_the_answer_has_no_service_response()
    {
        var sut = BuildSut((_, _) => Json(HttpStatusCode.OK, "[]"));

        var result = await sut.RunScriptWithResponseAsync("script.example_routine", null, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task RunScriptWithResponse_throws_HomeAssistantException_on_400()
    {
        var sut = BuildSut((_, _) => Json(HttpStatusCode.BadRequest, "400: Bad Request"));

        await Should.ThrowAsync<HomeAssistantException>(
            () => sut.RunScriptWithResponseAsync("script.example_routine", null, CancellationToken.None));
    }

    [Fact]
    public async Task RunScriptWithResponse_wraps_transport_exception_as_HomeAssistantException()
    {
        var inner = new HttpRequestException("connection reset");
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(inner);
        var sut = Build(handler.Object);

        var ex = await Should.ThrowAsync<HomeAssistantException>(
            () => sut.RunScriptWithResponseAsync("script.example_routine", null, CancellationToken.None));
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public async Task RunScriptWithResponse_lets_cancellation_through_unwrapped()
    {
        // The custom-skill handler tells its own timeout apart from a failure
        // by the OperationCanceledException; wrapping it would hide that.
        var sut = BuildSut((_, _) => throw new TaskCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.RunScriptWithResponseAsync("script.example_routine", null, CancellationToken.None));
    }

    [Theory]
    [InlineData("light.kitchen")]
    [InlineData("script.")]
    [InlineData("")]
    public async Task RunScriptWithResponse_rejects_a_non_script_entity_id(string entityId)
    {
        var sut = BuildSut((_, _) => Json(HttpStatusCode.OK, "{}"));

        await Should.ThrowAsync<ArgumentException>(
            () => sut.RunScriptWithResponseAsync(entityId, null, CancellationToken.None));
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

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
