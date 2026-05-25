using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Tests.Support;

namespace Lando.Alexa.SmartHome.Validators.Tests;

/// <summary>
/// <see cref="RequestValidator"/> enforces the inbound request envelope shape:
/// directive present, header present, payload version v3, and — for
/// device-targeted directives — an endpoint id plus a bearer token that
/// passes <see cref="ITokenValidator"/>. Skill-targeted directives
/// (<c>Discover</c>, <c>AcceptGrant</c>) skip the endpoint rules.
/// </summary>
public class RequestValidatorTests
{
    [Fact]
    public async Task Device_targeted_directive_with_valid_token_passes()
    {
        var sut = BuildSut(tokenValid: true);
        var request = RequestFixtures.Directive(
            Namespaces.PowerController, DirectiveNames.TurnOn,
            endpoint: RequestFixtures.Endpoint());

        (await sut.ValidateAsync(request)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Device_targeted_directive_with_invalid_token_fails()
    {
        var sut = BuildSut(tokenValid: false);
        var request = RequestFixtures.Directive(
            Namespaces.PowerController, DirectiveNames.TurnOn,
            endpoint: RequestFixtures.Endpoint());

        (await sut.ValidateAsync(request)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Discover_directive_skips_endpoint_and_token_rules()
    {
        // Token validator unreachable (would assert if called).
        var validator = new Mock<ITokenValidator>(MockBehavior.Strict);
        var sut = new RequestValidator(validator.Object);
        var request = RequestFixtures.Directive(
            Namespaces.Discovery, DirectiveNames.Discover,
            payload: new { scope = new { type = "BearerToken", token = "x" } });
        // No endpoint set.
        request.Directive.Endpoint = null;

        (await sut.ValidateAsync(request)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task AcceptGrant_directive_skips_endpoint_and_token_rules()
    {
        var validator = new Mock<ITokenValidator>(MockBehavior.Strict);
        var sut = new RequestValidator(validator.Object);
        var request = RequestFixtures.Directive(
            Namespaces.Authorization, DirectiveNames.AcceptGrant,
            payload: new { });
        request.Directive.Endpoint = null;

        (await sut.ValidateAsync(request)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Missing_endpoint_on_control_directive_fails()
    {
        var sut = BuildSut(tokenValid: true);
        var request = RequestFixtures.Directive(
            Namespaces.PowerController, DirectiveNames.TurnOn);
        request.Directive.Endpoint = null;

        var result = await sut.ValidateAsync(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Endpoint is required"));
    }

    [Fact]
    public async Task Unsupported_payload_version_fails()
    {
        var sut = BuildSut(tokenValid: true);
        var request = RequestFixtures.Directive(
            Namespaces.PowerController, DirectiveNames.TurnOn,
            endpoint: RequestFixtures.Endpoint());
        request.Directive.Header.PayloadVersion = "2";

        (await sut.ValidateAsync(request)).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Empty_namespace_or_name_fails(string? value)
    {
        var sut = BuildSut(tokenValid: true);
        var request = RequestFixtures.Directive(
            Namespaces.PowerController, DirectiveNames.TurnOn,
            endpoint: RequestFixtures.Endpoint());
        request.Directive.Header.Namespace = value!;

        (await sut.ValidateAsync(request)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Missing_endpoint_id_fails()
    {
        var sut = BuildSut(tokenValid: true);
        var request = RequestFixtures.Directive(
            Namespaces.PowerController, DirectiveNames.TurnOn,
            endpoint: RequestFixtures.Endpoint(endpointId: ""));

        (await sut.ValidateAsync(request)).IsValid.ShouldBeFalse();
    }

    private static RequestValidator BuildSut(bool tokenValid)
    {
        var validator = new Mock<ITokenValidator>();
        // Cover both overloads — the rule uses MustAsync on a SecureString-typed
        // property, so FluentValidation may resolve to either.
        validator.Setup(v => v.IsValidTokenAsync(It.IsAny<SecureString>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenValid);
        validator.Setup(v => v.IsValidTokenAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenValid);
        return new RequestValidator(validator.Object);
    }
}
