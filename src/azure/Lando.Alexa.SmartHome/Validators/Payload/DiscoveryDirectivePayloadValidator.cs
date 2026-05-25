using FluentValidation;
using Lando.Alexa.SmartHome.Models.Discovery;

namespace Lando.Alexa.SmartHome.Validators.Payload;

public class DiscoveryDirectivePayloadValidator : AbstractValidator<DiscoveryDirectivePayload>
{
    public DiscoveryDirectivePayloadValidator(ITokenValidator tokenValidator)
    {
        RuleFor(r => r.Scope).NotNull().WithMessage("Scope is required for discovery directives");
        When(r => r.Scope != null, () =>
        {
            RuleFor(r => r.Scope!.Token.Value)
                .NotEmpty().WithMessage("Invalid authentication token")
                .MustAsync(tokenValidator.IsValidTokenAsync).WithMessage("Invalid authentication token");
        });
    }
}
