using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

public class SetBrightnessPayloadValidator : AbstractValidator<SetBrightnessPayload>
{
    public SetBrightnessPayloadValidator()
    {
        RuleFor(p => p.Brightness).InclusiveBetween(0, 100).WithMessage("Brightness must be between 0 and 100");
    }
}
