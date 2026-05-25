using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;

namespace Lando.HomeAssistant.Services.Tests;

/// <summary>
/// <see cref="DeviceDiscoveryService"/> wraps the HA <c>states</c> REST
/// endpoint. Tests pin the streaming behaviour, the GET-by-id 404 contract,
/// the argument validation, and the failure translation into
/// <see cref="HomeAssistantException"/>.
/// </summary>
public class DeviceDiscoveryServiceTests
{
    [Fact]
    public async Task ListAsync_streams_every_entity_in_order()
    {
        const string body = """
            [
                { "entity_id": "light.kitchen", "state": "on", "attributes": {} },
                { "entity_id": "switch.outlet", "state": "off", "attributes": {} }
            ]
            """;
        var captured = new List<HttpRequestMessage>();
        var sut = BuildSut(Json(body), captured);

        var entities = new List<string>();
        await foreach (var e in sut.ListAsync(CancellationToken.None))
            entities.Add(e.EntityId);

        entities.ShouldBe(["light.kitchen", "switch.outlet"]);
        captured[0].RequestUri!.AbsolutePath.ShouldEndWith("states");
    }

    [Fact]
    public async Task ListAsync_skips_null_entities()
    {
        const string body = """
            [
                { "entity_id": "light.kitchen", "state": "on", "attributes": {} },
                null,
                { "entity_id": "switch.outlet", "state": "off", "attributes": {} }
            ]
            """;
        var sut = BuildSut(Json(body));

        var entities = new List<string>();
        await foreach (var e in sut.ListAsync(CancellationToken.None))
            entities.Add(e.EntityId);

        entities.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_throws_HomeAssistantException_on_non_success_status()
    {
        var sut = BuildSut(Status(HttpStatusCode.Unauthorized, "no"));

        await Should.ThrowAsync<HomeAssistantException>(async () =>
        {
            await foreach (var _ in sut.ListAsync(CancellationToken.None))
            { }
        });
    }

    [Fact]
    public async Task GetAsync_returns_entity_on_success()
    {
        var captured = new List<HttpRequestMessage>();
        var sut = BuildSut(Json("""{ "entity_id": "light.kitchen", "state": "on", "attributes": {} }"""), captured);

        var entity = await sut.GetAsync("light.kitchen", CancellationToken.None);

        entity.ShouldNotBeNull();
        entity!.EntityId.ShouldBe("light.kitchen");
        captured[0].RequestUri!.AbsolutePath.ShouldEndWith("states/light.kitchen");
    }

    [Fact]
    public async Task GetAsync_returns_null_on_not_found()
    {
        var sut = BuildSut(Status(HttpStatusCode.NotFound));

        (await sut.GetAsync("light.does_not_exist", CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_throws_HomeAssistantException_on_other_failure_status()
    {
        var sut = BuildSut(Status(HttpStatusCode.InternalServerError, "boom"));

        await Should.ThrowAsync<HomeAssistantException>(() => sut.GetAsync("light.kitchen", CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_throws_ArgumentException_for_blank_entity_id(string entityId)
    {
        var captured = new List<HttpRequestMessage>();
        var sut = BuildSut(Status(HttpStatusCode.OK), captured);

        await Should.ThrowAsync<ArgumentException>(() => sut.GetAsync(entityId, CancellationToken.None));
        captured.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAsync_wraps_transport_exception_as_HomeAssistantException()
    {
        var inner = new HttpRequestException("connection reset");
        var sut = BuildSutThrowing(inner);

        var ex = await Should.ThrowAsync<HomeAssistantException>(
            () => sut.GetAsync("light.kitchen", CancellationToken.None));
        ex.InnerException.ShouldBeSameAs(inner);
    }

    private static DeviceDiscoveryService BuildSut(HttpResponseMessage response, List<HttpRequestMessage>? captured = null)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                captured?.Add(req);
                // Each request needs its own response copy because the framework disposes them.
                return Task.FromResult(Clone(response));
            });
        return Build(handler.Object);
    }

    private static DeviceDiscoveryService BuildSutThrowing(Exception exception)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
        return Build(handler.Object);
    }

    private static DeviceDiscoveryService Build(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://homeassistant.example.local:8123/api/") };
        return new DeviceDiscoveryService(http, NullLogger<DeviceDiscoveryService>.Instance);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage Status(HttpStatusCode status, string? body = null)
    {
        var response = new HttpResponseMessage(status);
        if (body is not null)
            response.Content = new StringContent(body);
        return response;
    }

    /// <summary>
    /// Shallow clone — fresh content stream so the framework can dispose it
    /// without breaking subsequent requests in the same test.
    /// </summary>
    private static HttpResponseMessage Clone(HttpResponseMessage source)
    {
        var copy = new HttpResponseMessage(source.StatusCode);
        if (source.Content is not null)
        {
            var body = source.Content.ReadAsStringAsync().Result;
            copy.Content = new StringContent(body, Encoding.UTF8,
                source.Content.Headers.ContentType?.MediaType ?? "text/plain");
        }
        return copy;
    }
}
