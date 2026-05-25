using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;

namespace Lando.Alexa.SmartHome.Validators.Payload.Tests;

/// <summary>
/// <see cref="DiscoveryDirectivePayloadValidator"/> enforces that a Discover
/// payload carries a scope with a bearer token that <see cref="ITokenValidator"/>
/// accepts.
/// </summary>
public class DiscoveryDirectivePayloadValidatorTests
{
    [Fact]
    public async Task Rejects_payload_with_null_scope()
    {
        var sut = new DiscoveryDirectivePayloadValidator(TokenValidator(true));
        var payload = new DiscoveryDirectivePayload { Scope = null };

        (await sut.ValidateAsync(payload)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Rejects_payload_with_empty_token()
    {
        var sut = new DiscoveryDirectivePayloadValidator(TokenValidator(true));
        var payload = new DiscoveryDirectivePayload { Scope = new Scope { Token = new SecureString(null) } };

        (await sut.ValidateAsync(payload)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Rejects_payload_when_token_validator_rejects()
    {
        var sut = new DiscoveryDirectivePayloadValidator(TokenValidator(false));
        var payload = new DiscoveryDirectivePayload { Scope = new Scope { Token = new SecureString("bad") } };

        (await sut.ValidateAsync(payload)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Accepts_payload_with_valid_token()
    {
        var sut = new DiscoveryDirectivePayloadValidator(TokenValidator(true));
        var payload = new DiscoveryDirectivePayload { Scope = new Scope { Token = new SecureString("good") } };

        (await sut.ValidateAsync(payload)).IsValid.ShouldBeTrue();
    }

    private static ITokenValidator TokenValidator(bool valid)
    {
        var mock = new Mock<ITokenValidator>();
        mock.Setup(v => v.IsValidTokenAsync(It.IsAny<SecureString>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(valid);
        mock.Setup(v => v.IsValidTokenAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(valid);
        return mock.Object;
    }
}
