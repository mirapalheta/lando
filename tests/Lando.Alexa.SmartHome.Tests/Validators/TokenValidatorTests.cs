using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.Security.LWA;
using Lando.Alexa.SmartHome.Configuration;
using Lando.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq.Protected;

namespace Lando.Alexa.SmartHome.Validators.Tests;

/// <summary>
/// <see cref="TokenValidator"/> introspects an LWA bearer token, pins its
/// <c>aud</c> against the configured skill client id, and caches the result.
/// Tests pin the happy path, every failure branch (null/wrong aud / non-
/// positive exp / transport exception), and the cache reuse for repeated
/// calls on the same token.
/// </summary>
public class TokenValidatorTests
{
    private const string SkillClientId = "amzn1.application-oa2-client.SKILL";

    [Fact]
    public async Task Returns_true_for_valid_token()
    {
        var (sut, sendMock) = BuildSut(skillClientId: SkillClientId,
            tokenInfo: """{"aud":"amzn1.application-oa2-client.SKILL","exp":3600}""");

        (await sut.IsValidTokenAsync("ya29.tok", CancellationToken.None)).ShouldBeTrue();
        sendMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Returns_false_when_token_is_null_or_whitespace()
    {
        var (sut, _) = BuildSut(skillClientId: SkillClientId,
            tokenInfo: """{"aud":"amzn1.application-oa2-client.SKILL","exp":3600}""");

        (await sut.IsValidTokenAsync(default(string?), CancellationToken.None)).ShouldBeFalse();
        (await sut.IsValidTokenAsync("   ", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task Returns_false_when_aud_does_not_match_configured_client_id()
    {
        var (sut, _) = BuildSut(skillClientId: SkillClientId,
            tokenInfo: """{"aud":"amzn1.application-oa2-client.OTHER","exp":3600}""");

        (await sut.IsValidTokenAsync("ya29.other", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task Returns_false_when_aud_is_missing()
    {
        var (sut, _) = BuildSut(skillClientId: SkillClientId,
            tokenInfo: """{"exp":3600}""");

        (await sut.IsValidTokenAsync("ya29.malformed", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task Returns_false_when_exp_is_zero_or_negative()
    {
        var (sut, _) = BuildSut(skillClientId: SkillClientId,
            tokenInfo: """{"aud":"amzn1.application-oa2-client.SKILL","exp":0}""");

        (await sut.IsValidTokenAsync("ya29.expired", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task Returns_false_when_introspection_throws()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("boom"));
        var sut = Build(handler.Object, SkillClientId);

        (await sut.IsValidTokenAsync("ya29.broken", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task Caches_result_so_second_call_does_not_hit_LWA()
    {
        var (sut, sendMock) = BuildSut(skillClientId: SkillClientId,
            tokenInfo: """{"aud":"amzn1.application-oa2-client.SKILL","exp":3600}""");

        (await sut.IsValidTokenAsync("ya29.cached", CancellationToken.None)).ShouldBeTrue();
        (await sut.IsValidTokenAsync("ya29.cached", CancellationToken.None)).ShouldBeTrue();

        sendMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SecureString_overload_delegates_to_string_overload()
    {
        var (sut, _) = BuildSut(skillClientId: SkillClientId,
            tokenInfo: """{"aud":"amzn1.application-oa2-client.SKILL","exp":3600}""");

        (await sut.IsValidTokenAsync(new SecureString("ya29.tok"), CancellationToken.None)).ShouldBeTrue();
    }

    private static (ITokenValidator Sut, Mock<HttpMessageHandler> Handler) BuildSut(
        string skillClientId, string tokenInfo)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(tokenInfo, Encoding.UTF8, "application/json"),
            });
        return (Build(handler.Object, skillClientId), handler);
    }

    private static TokenValidator Build(HttpMessageHandler handler, string skillClientId)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.amazon.com/auth/o2/") };
        var lwa = new LwaTokenClient(http,
            new ClientCredentials { ClientId = "x", ClientSecret = "y" },
            NullLogger<LwaTokenClient>.Instance);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new SmartHomeOptions
        {
            Authorization = new ClientCredentials { ClientId = skillClientId, ClientSecret = "skill-secret" },
        });
        return new TokenValidator(lwa, cache, options, NullLogger<TokenValidator>.Instance);
    }
}
