using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

public class SetPercentagePayloadValidator : AbstractValidator<SetPercentagePayload>
{
    public SetPercentagePayloadValidator()
    {
        RuleFor(p => p.Percentage).InclusiveBetween(0, 100).WithMessage("Percentage must be between 0 and 100");
    }
}
