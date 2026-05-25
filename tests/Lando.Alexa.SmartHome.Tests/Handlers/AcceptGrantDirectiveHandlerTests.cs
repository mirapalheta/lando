using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.Security.LWA;
using Lando.Alexa.SmartHome.Models.Authorization;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Tests.Support;
using Lando.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives.Tests;

/// <summary>
/// <see cref="AcceptGrantDirectiveHandler"/> runs once per skill-enable. The
/// three-step flow (introspect grantee, exchange code, persist refresh token)
/// each have failure branches that must surface as
/// <see cref="ErrorType.AcceptGrantFailed"/>.
/// </summary>
public class AcceptGrantDirectiveHandlerTests
{
    private static readonly IOptions<JsonSerializerOptions> JsonOptions = Options.Create(new JsonSerializerOptions());

    /// <summary>
    /// Happy path: introspect resolves user_id, exchanged token includes
    /// refresh_token, store records (user_id, refresh).
    /// </summary>
    [Fact]
    public async Task Persists_refresh_token_and_returns_AcceptGrantResponse()
    {
        var (sut, store, client) = BuildSut();
        client.Setup(c => c.GetAsync("grantee-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenInfo { UserId = "amzn1.account.ABC" });
        client.Setup(c => c.ExchangeCodeAsync("code-from-alexa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Token { AccessToken = "at", RefreshToken = "rt", ExpiresIn = 3600 });

        var response = await sut.HandleAsync(BuildRequest(), CancellationToken.None);

        response.Event.Header.Namespace.ShouldBe(Namespaces.Authorization);
        response.Event.Header.Name.ShouldBe(EventNames.AcceptGrantResponse);
        store.Verify(s => s.SaveAsync("amzn1.account.ABC", "rt", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Throws_AcceptGrantFailed_when_introspect_returns_null()
    {
        var (sut, _, client) = BuildSut();
        client.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenInfo?)null);

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.AcceptGrantFailed);
    }

    [Fact]
    public async Task Throws_AcceptGrantFailed_when_introspect_returns_empty_user_id()
    {
        var (sut, _, client) = BuildSut();
        client.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenInfo { UserId = "" });

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.AcceptGrantFailed);
    }

    [Fact]
    public async Task Throws_AcceptGrantFailed_when_exchange_returns_no_refresh_token()
    {
        var (sut, _, client) = BuildSut();
        client.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenInfo { UserId = "amzn1.account.ABC" });
        client.Setup(c => c.ExchangeCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Token { AccessToken = "at", RefreshToken = null, ExpiresIn = 3600 });

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.AcceptGrantFailed);
    }

    [Fact]
    public async Task Wraps_LwaTokenException_as_AcceptGrantFailed()
    {
        var inner = new LwaTokenException("upstream said 401");
        var (sut, _, client) = BuildSut();
        client.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenInfo { UserId = "amzn1.account.ABC" });
        client.Setup(c => c.ExchangeCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(inner);

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.AcceptGrantFailed);
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public async Task Wraps_unexpected_exception_as_AcceptGrantFailed()
    {
        var inner = new InvalidOperationException("disk full");
        var (sut, _, client) = BuildSut();
        client.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenInfo { UserId = "amzn1.account.ABC" });
        client.Setup(c => c.ExchangeCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(inner);

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.AcceptGrantFailed);
        ex.InnerException.ShouldBeSameAs(inner);
    }

    /// <summary>
    /// Composes the SUT around mocked <see cref="ITokenStore"/> + nested
    /// <see cref="ITokenClient"/>.
    /// </summary>
    private static (AcceptGrantDirectiveHandler Sut, Mock<ITokenStore> Store, Mock<ITokenClient> Client) BuildSut()
    {
        var client = new Mock<ITokenClient>();
        var store = new Mock<ITokenStore>();
        store.SetupGet(s => s.Client).Returns(client.Object);
        store.Setup(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = new AcceptGrantDirectiveHandler(validator: null!, JsonOptions, store.Object,
            NullLogger<AcceptGrantDirectiveHandler>.Instance);
        return (sut, store, client);
    }

    private static Request BuildRequest()
        => RequestFixtures.Directive(
            Namespaces.Authorization, DirectiveNames.AcceptGrant,
            payload: new AcceptGrantPayload
            {
                Grant = new Grant { Type = GrantType.OAuth2AuthorizationCode, Code = "code-from-alexa" },
                Grantee = new Grantee { Type = "BearerToken", Token = new SecureString("grantee-token") },
            });
}
