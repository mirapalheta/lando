using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq.Protected;

namespace Lando.Alexa.SmartHome.Services.Tests;

/// <summary>
/// <see cref="EventGatewayClient"/> fans a ChangeReport out to every grantee
/// in <see cref="ITokenStore"/> and aggregates per-recipient success bits.
/// Tests pin URL/method/bearer header, the fan-out + bearer-per-grantee
/// contract, the aggregate-false-on-any-fail rule, and transport-exception
/// swallowing.
/// </summary>
public class EventGatewayClientTests
{
    private const string EndpointId = "light#kitchen";
    private static readonly IOptions<JsonSerializerOptions> JsonOptions = Options.Create(new JsonSerializerOptions());

    [Fact]
    public async Task Posts_ChangeReport_to_v3_events_with_bearer_token()
    {
        var captured = new System.Collections.Generic.List<(HttpRequestMessage, string)>();
        var handler = NewHandler((req, _) =>
        {
            captured.Add((req, req.Content!.ReadAsStringAsync().Result));
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var store = Store(("amzn1.account.A", "access-A"));
        var sut = BuildSut(handler.Object, store);

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeTrue();
        var (sent, body) = captured.ShouldHaveSingleItem();
        sent.Method.ShouldBe(HttpMethod.Post);
        sent.RequestUri!.AbsolutePath.ShouldEndWith("v3/events");
        sent.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        sent.Headers.Authorization.Parameter.ShouldBe("access-A");
        body.ShouldContain(EndpointId);
        body.ShouldContain("powerState");
    }

    [Fact]
    public async Task Fans_out_one_post_per_grantee_with_per_grantee_bearer()
    {
        var captured = new System.Collections.Generic.List<HttpRequestMessage>();
        var handler = NewHandler((req, _) =>
        {
            captured.Add(req);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var store = Store(("amzn1.account.A", "access-A"), ("amzn1.account.B", "access-B"));
        var sut = BuildSut(handler.Object, store);

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeTrue();
        captured.Count.ShouldBe(2);
        captured.Select(r => r.Headers.Authorization!.Parameter).ToHashSet()
            .ShouldBe(new System.Collections.Generic.HashSet<string?> { "access-A", "access-B" });
    }

    [Fact]
    public async Task Returns_false_when_any_grantee_fails()
    {
        var i = 0;
        var handler = NewHandler((_, _) =>
            Interlocked.Increment(ref i) == 1
                ? new HttpResponseMessage(HttpStatusCode.Accepted)
                : new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var store = Store(("a", "x"), ("b", "y"));
        var sut = BuildSut(handler.Object, store);

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeFalse();
    }

    [Fact]
    public async Task Swallows_transport_exception_and_returns_false()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));
        var sut = BuildSut(handler.Object, Store(("a", "x")));

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeFalse();
    }

    [Fact]
    public async Task Returns_true_and_makes_no_calls_when_no_grantees()
    {
        var handler = NewHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Accepted));
        var sut = BuildSut(handler.Object, Store());

        var ok = await sut.SendChangeReportAsync(EndpointId, [Property("powerState", "ON")],
            [Property("powerState", "ON")], CancellationToken.None);

        ok.ShouldBeTrue();
        handler.Protected().Verify(
            "SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    private static Mock<HttpMessageHandler> NewHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => respond(req, ct));
        return handler;
    }

    private static ITokenStore Store(params (string userId, string accessToken)[] tokens)
    {
        var store = new Mock<ITokenStore>();
        store.Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);
        return store.Object;
    }

    private static EventGatewayClient BuildSut(HttpMessageHandler handler, ITokenStore store)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.amazonalexa.com/") };
        return new EventGatewayClient(http, store, JsonOptions, NullLogger<EventGatewayClient>.Instance);
    }

    private static ContextProperty Property(string name, object value) => new()
    {
        Namespace = Namespaces.PowerController,
        Name = name,
        Value = value,
        TimeOfSample = DateTime.UtcNow,
    };
}
