using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;

namespace Lando.Alexa.SmartHome.Validators.Payload;

public class SetVolumePayloadValidator : AbstractValidator<SetVolumePayload>
{
    public SetVolumePayloadValidator()
    {
        RuleFor(p => p.Volume).InclusiveBetween(0, 100).WithMessage("Volume must be between 0 and 100");
    }
}
