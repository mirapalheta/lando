using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Lando.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;

namespace Lando.Alexa.Security.LWA.Tests;

/// <summary>
/// <see cref="LwaTokenClient"/> talks to Amazon LWA endpoints — tokeninfo +
/// code exchange + refresh. Tests pin wire shape (form-urlencoded body,
/// client credentials), success deserialisation, and failure translation
/// into <see cref="LwaTokenException"/>. The introspection call has its own
/// permissive contract: any failure returns null.
/// </summary>
public class LwaTokenClientTests
{
    private static readonly ClientCredentials Credentials = new() { ClientId = "client-id", ClientSecret = "client-secret" };

    [Fact]
    public async Task ExchangeCode_posts_form_body_and_returns_token()
    {
        var captured = new List<HttpRequestMessage>();
        var captureBodies = new List<string>();
        var handler = NewHandler(async (req, ct) =>
        {
            captured.Add(req);
            captureBodies.Add(await req.Content!.ReadAsStringAsync(ct));
            return Json("""{"access_token":"at","refresh_token":"rt","token_type":"bearer","expires_in":3600}""");
        });
        var sut = BuildSut(handler.Object);

        var token = await sut.ExchangeCodeAsync("the-code", CancellationToken.None);

        token.AccessToken.ShouldBe("at");
        token.RefreshToken.ShouldBe("rt");
        token.ExpiresIn.ShouldBe(3600);

        var form = ParseForm(captureBodies[0]);
        form["grant_type"].ShouldBe("authorization_code");
        form["code"].ShouldBe("the-code");
        form["client_id"].ShouldBe("client-id");
        form["client_secret"].ShouldBe("client-secret");
    }

    [Fact]
    public async Task Refresh_posts_form_body_and_returns_token()
    {
        var bodies = new List<string>();
        var handler = NewHandler(async (req, ct) =>
        {
            bodies.Add(await req.Content!.ReadAsStringAsync(ct));
            return Json("""{"access_token":"new-at","token_type":"bearer","expires_in":3600}""");
        });
        var sut = BuildSut(handler.Object);

        var token = await sut.RefreshAsync("stored-rt", CancellationToken.None);

        token.AccessToken.ShouldBe("new-at");
        token.RefreshToken.ShouldBeNull();

        var form = ParseForm(bodies[0]);
        form["grant_type"].ShouldBe("refresh_token");
        form["refresh_token"].ShouldBe("stored-rt");
    }

    [Fact]
    public async Task ExchangeCode_throws_LwaTokenException_on_non_success_status()
    {
        var handler = NewHandler((_, _) => Task.FromResult(Status(HttpStatusCode.Unauthorized, """{"error":"invalid_client"}""")));
        var sut = BuildSut(handler.Object);

        await Should.ThrowAsync<LwaTokenException>(
            () => sut.ExchangeCodeAsync("bad-code", CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCode_throws_LwaTokenException_when_access_token_missing()
    {
        var handler = NewHandler((_, _) => Task.FromResult(Json("""{"token_type":"bearer","expires_in":3600}""")));
        var sut = BuildSut(handler.Object);

        await Should.ThrowAsync<LwaTokenException>(
            () => sut.ExchangeCodeAsync("the-code", CancellationToken.None));
    }

    [Fact]
    public async Task Get_returns_TokenInfo_on_success()
    {
        var captured = new List<HttpRequestMessage>();
        var handler = NewHandler((req, _) =>
        {
            captured.Add(req);
            return Task.FromResult(Json("""{"iss":"https://www.amazon.com","user_id":"amzn1.account.ABC","aud":"client-id","app_id":"app","exp":3600}"""));
        });
        var sut = BuildSut(handler.Object);

        var info = await sut.GetAsync("bearer-token-xyz", CancellationToken.None);

        info.ShouldNotBeNull();
        info!.UserId.ShouldBe("amzn1.account.ABC");
        info.Aud.ShouldBe("client-id");

        captured[0].Method.ShouldBe(HttpMethod.Get);
        captured[0].RequestUri!.AbsolutePath.ShouldEndWith("tokeninfo");
        HttpUtility.ParseQueryString(captured[0].RequestUri!.Query)["access_token"].ShouldBe("bearer-token-xyz");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Get_returns_null_on_failure_status(HttpStatusCode status)
    {
        var handler = NewHandler((_, _) => Task.FromResult(Status(status)));
        var sut = BuildSut(handler.Object);

        var info = await sut.GetAsync("token", CancellationToken.None);

        info.ShouldBeNull();
    }

    [Fact]
    public async Task Get_returns_null_on_transport_exception()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("DNS failure"));
        var sut = BuildSut(handler.Object);

        var info = await sut.GetAsync("token", CancellationToken.None);

        info.ShouldBeNull();
    }

    private static Mock<HttpMessageHandler> NewHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, ct) => respond(req, ct));
        return handler;
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

    private static LwaTokenClient BuildSut(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.amazon.com/auth/o2/") };
        return new LwaTokenClient(http, Credentials, NullLogger<LwaTokenClient>.Instance);
    }

    private static IDictionary<string, string?> ParseForm(string body)
    {
        var parsed = HttpUtility.ParseQueryString(body, Encoding.UTF8);
        var dict = new Dictionary<string, string?>();
        foreach (string key in parsed.Keys)
            dict[key] = parsed[key];
        return dict;
    }
}
