using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

public class SetColorPayloadValidator : AbstractValidator<SetColorPayload>
{
    public SetColorPayloadValidator()
    {
        RuleFor(p => p.Color).NotNull().WithMessage("Color is required");
        When(p => p.Color is not null, () =>
        {
            RuleFor(p => p.Color.Brightness).InclusiveBetween(0, 1).WithMessage("Color brightness must be between 0 and 1");
            RuleFor(p => p.Color.Hue).InclusiveBetween(0, 360).WithMessage("Color hue must be between 0 and 360");
            RuleFor(p => p.Color.Saturation).InclusiveBetween(0, 1).WithMessage("Color saturation must be between 0 and 1");
        });
    }
}
