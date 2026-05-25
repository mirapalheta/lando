using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

public class SetRangeValuePayloadValidator : AbstractValidator<SetRangeValuePayload>
{
    public SetRangeValuePayloadValidator()
    {
        RuleFor(p => p.RangeValue).InclusiveBetween(0, 100).WithMessage("RangeValue must be between 0 and 100");
    }
}
