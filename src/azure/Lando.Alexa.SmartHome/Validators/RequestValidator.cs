using FluentValidation;
using Lando.Alexa.SmartHome.Models.Core;

namespace Lando.Alexa.SmartHome.Validators;

/// <summary>
/// Structural validation for the inbound Smart Home <see cref="Request"/>. Enforces
/// header presence, payload version, and the per-directive rule that device-targeted
/// directives carry an endpoint with a bearer token, while skill-targeted directives
/// (<c>Discover</c>, <c>AcceptGrant</c>) do not.
/// </summary>
public class RequestValidator : AbstractValidator<Request>
{
    /// <summary>
    /// Builds the FluentValidation rule chain for inbound Smart Home requests.
    /// The token validator is invoked asynchronously by the rule chain when a
    /// device-targeted directive carries a bearer token.
    /// </summary>
    /// <param name="tokenValidator">
    /// Validates the <c>endpoint.scope.token</c> bearer token against LWA
    /// (typically <c>TokenValidator</c> with the configured skill client_id).
    /// </param>
    public RequestValidator(ITokenValidator tokenValidator)
    {
        RuleFor(r => r.Directive).NotNull().WithMessage("Directive is required");
        When(r => r.Directive != null, () =>
        {
            RuleFor(r => r.Directive.Header).NotNull().WithMessage("Directive header is required");
            When(r => r.Directive.Header != null, () =>
            {
                RuleFor(r => r.Directive.Header.Namespace).NotEmpty().WithMessage("Directive header namespace is required");
                RuleFor(r => r.Directive.Header.Name).NotEmpty().WithMessage("Directive header name is required");
                RuleFor(r => r.Directive.Header.PayloadVersion)
                    .NotEmpty().WithMessage("Payload version is required")
                    .Must(v => v == PayloadVersion.V3).WithMessage($"Unsupported payload version. Only version {PayloadVersion.V3} is supported");

                // Discover and AcceptGrant target the skill itself — no endpoint on the
                // directive. Every other directive must carry an endpoint and a bearer
                // token Alexa can present on the customer's behalf.
                When(IsDeviceTargeted, () =>
                {
                    RuleFor(r => r.Directive.Endpoint).NotNull().WithMessage("Endpoint is required for control directives");
                    When(r => r.Directive.Endpoint != null, () =>
                    {
                        RuleFor(r => r.Directive.Endpoint!.EndpointId).NotEmpty().WithMessage("Endpoint id is required for control directives");
                        RuleFor(r => r.Directive.Endpoint!.Scope).NotNull().WithMessage("Endpoint scope is required for control directives");
                        When(r => r.Directive.Endpoint!.Scope != null, () =>
                        {
                            RuleFor(r => r.Directive.Endpoint!.Scope!.Token.Value)
                                .NotEmpty().WithMessage("Invalid authentication token")
                                .MustAsync(tokenValidator.IsValidTokenAsync).WithMessage("Invalid authentication token");
                        });
                    });
                });
            });
        });
    }

    private static bool IsDeviceTargeted(Request request)
    {
        var name = request.Directive.Header.Name;
        return name != DirectiveNames.Discover
            && name != DirectiveNames.AcceptGrant;
    }
}
