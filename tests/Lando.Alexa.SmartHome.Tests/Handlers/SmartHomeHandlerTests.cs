using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Tests;

/// <summary>
/// <see cref="SmartHomeHandler"/> is the top-level dispatcher: every inbound
/// Smart Home directive flows through it before reaching a per-directive
/// handler. The tests below pin the dispatcher's contract — directive
/// resolution by keyed-service name, validator integration, exception-to-error
/// translation — without depending on any concrete directive handler.
/// </summary>
public class SmartHomeHandlerTests
{
    private static readonly IOptions<JsonSerializerOptions> JsonOptions = Options.Create(new JsonSerializerOptions());

    /// <summary>
    /// Happy path: a directive whose name matches a registered keyed
    /// <c>IDirectiveHandler</c> is invoked exactly once and its response is
    /// returned unchanged.
    /// </summary>
    [Fact]
    public async Task Dispatches_to_keyed_handler_and_returns_its_response()
    {
        var inner = new Mock<IDirectiveHandler>();
        inner.SetupGet(h => h.DirectiveName).Returns(DirectiveNames.TurnOn);
        inner.Setup(h => h.HandleAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse());
        var sut = BuildSut(inner.Object);
        var request = RequestFixtures.Directive(Namespaces.PowerController, DirectiveNames.TurnOn,
            endpoint: RequestFixtures.Endpoint());

        var response = await sut.HandleAsync(request, CancellationToken.None);

        inner.Verify(h => h.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        response.Event.Header.Namespace.ShouldBe(Namespaces.Alexa);
        response.Event.Header.Name.ShouldBe(EventNames.Response);
    }

    /// <summary>
    /// Asserts the inbound <see cref="CancellationToken"/> is forwarded to the
    /// inner handler — losing it would silently break the function host's
    /// ability to abort overlong directive work.
    /// </summary>
    [Fact]
    public async Task Forwards_cancellation_token_to_inner_handler()
    {
        using var cts = new CancellationTokenSource();
        var inner = new Mock<IDirectiveHandler>();
        inner.SetupGet(h => h.DirectiveName).Returns(DirectiveNames.TurnOn);
        inner.Setup(h => h.HandleAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse());
        var sut = BuildSut(inner.Object);

        await sut.HandleAsync(RequestFixtures.Directive(Namespaces.PowerController, DirectiveNames.TurnOn,
            endpoint: RequestFixtures.Endpoint()), cts.Token);

        inner.Verify(h => h.HandleAsync(It.IsAny<Request>(), cts.Token), Times.Once);
    }

    /// <summary>
    /// No handler registered for the directive name → <c>INVALID_DIRECTIVE</c>
    /// error response (Alexa requires a well-formed envelope even for
    /// unsupported directives).
    /// </summary>
    [Fact]
    public async Task Returns_InvalidDirective_when_no_handler_is_registered()
    {
        var sut = BuildSut(); // no handlers registered
        var request = RequestFixtures.Directive(Namespaces.PowerController, DirectiveNames.TurnOn,
            endpoint: RequestFixtures.Endpoint());

        var response = await sut.HandleAsync(request, CancellationToken.None);

        ShouldBeErrorResponse(response, ErrorType.InvalidDirective);
    }

    /// <summary>
    /// FluentValidation failure → <c>INVALID_DIRECTIVE</c>. The inner handler
    /// must not be invoked.
    /// </summary>
    [Fact]
    public async Task Returns_InvalidDirective_when_validator_fails()
    {
        var inner = new Mock<IDirectiveHandler>(MockBehavior.Strict);
        inner.SetupGet(h => h.DirectiveName).Returns(DirectiveNames.TurnOn);
        var validator = new Mock<IValidator<Request>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<Request>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException([new ValidationFailure("Directive", "stub failure")]));
        var sut = BuildSut(inner.Object, validator.Object);

        var response = await sut.HandleAsync(
            RequestFixtures.Directive(Namespaces.PowerController, DirectiveNames.TurnOn,
                endpoint: RequestFixtures.Endpoint()),
            CancellationToken.None);

        ShouldBeErrorResponse(response, ErrorType.InvalidDirective);
        inner.Verify(h => h.HandleAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// <see cref="AlexaSmartHomeException"/> from a handler is translated into
    /// an error response whose <c>type</c> matches the exception's
    /// <see cref="ErrorType"/>.
    /// </summary>
    /// <param name="error">The error to throw and assert on.</param>
    [Theory]
    [InlineData(ErrorType.NoSuchEndpoint)]
    [InlineData(ErrorType.EndpointUnreachable)]
    [InlineData(ErrorType.InvalidValue)]
    public async Task Translates_AlexaSmartHomeException_into_matching_error_response(ErrorType error)
    {
        var inner = new Mock<IDirectiveHandler>();
        inner.SetupGet(h => h.DirectiveName).Returns(DirectiveNames.TurnOn);
        inner.Setup(h => h.HandleAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AlexaSmartHomeException(error, "device unreachable"));
        var sut = BuildSut(inner.Object);

        var response = await sut.HandleAsync(
            RequestFixtures.Directive(Namespaces.PowerController, DirectiveNames.TurnOn,
                endpoint: RequestFixtures.Endpoint()),
            CancellationToken.None);

        ShouldBeErrorResponse(response, error);
    }

    /// <summary>
    /// Any other exception type from the handler must surface as
    /// <c>INTERNAL_ERROR</c> rather than escape — last defence before the
    /// Azure Functions host translates it into an unparseable HTTP 500.
    /// </summary>
    [Fact]
    public async Task Translates_unexpected_exception_into_InternalError()
    {
        var inner = new Mock<IDirectiveHandler>();
        inner.SetupGet(h => h.DirectiveName).Returns(DirectiveNames.TurnOn);
        inner.Setup(h => h.HandleAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var sut = BuildSut(inner.Object);

        var response = await sut.HandleAsync(
            RequestFixtures.Directive(Namespaces.PowerController, DirectiveNames.TurnOn,
                endpoint: RequestFixtures.Endpoint()),
            CancellationToken.None);

        ShouldBeErrorResponse(response, ErrorType.InternalError);
    }

    /// <summary>
    /// Error responses preserve endpoint id + correlation token so Alexa can
    /// correlate the failure with the originating request.
    /// </summary>
    [Fact]
    public async Task Error_response_preserves_endpoint_and_correlation_token()
    {
        var inner = new Mock<IDirectiveHandler>();
        inner.SetupGet(h => h.DirectiveName).Returns(DirectiveNames.TurnOn);
        inner.Setup(h => h.HandleAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AlexaSmartHomeException(ErrorType.EndpointUnreachable, "x"));
        var sut = BuildSut(inner.Object);

        var response = await sut.HandleAsync(RequestFixtures.Directive(
            Namespaces.PowerController, DirectiveNames.TurnOn,
            endpoint: RequestFixtures.Endpoint(endpointId: "light#kitchen"),
            correlationToken: "corr-xyz"), CancellationToken.None);

        response.Event.Endpoint.ShouldNotBeNull();
        response.Event.Endpoint!.EndpointId.ShouldBe("light#kitchen");
        response.Event.Header.CorrelationToken.Value.ShouldBe("corr-xyz");
    }

    /// <summary>
    /// Builds the SUT around an optional keyed inner handler + an always-pass
    /// validator unless one is supplied.
    /// </summary>
    private static SmartHomeHandler BuildSut(IDirectiveHandler? inner = null, IValidator<Request>? validator = null)
    {
        var services = new ServiceCollection();
        if (inner is not null)
            services.AddKeyedSingleton(inner.DirectiveName, inner);

        if (validator is null)
        {
            var pass = new Mock<IValidator<Request>>();
            pass.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<Request>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            validator = pass.Object;
        }

        return new SmartHomeHandler(services.BuildServiceProvider(), validator, JsonOptions,
            NullLogger<SmartHomeHandler>.Instance);
    }

    private static Response SuccessResponse() => new()
    {
        Event = Event.Create(Namespaces.Alexa, EventNames.Response, EmptyPayload.Instance),
    };

    private static void ShouldBeErrorResponse(Response response, ErrorType expected)
    {
        response.Event.Header.Namespace.ShouldBe(Namespaces.Alexa);
        response.Event.Header.Name.ShouldBe(EventNames.ErrorResponse);
        var payload = response.Event.Payload.ShouldBeOfType<ErrorPayload>();
        payload.Type.ShouldBe(expected.ToErrorCode());
    }
}
