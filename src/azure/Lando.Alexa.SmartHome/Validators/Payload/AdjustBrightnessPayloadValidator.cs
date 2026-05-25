using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates <see cref="AdjustBrightnessPayload"/>: requires the
/// <see cref="AdjustBrightnessPayload.BrightnessDelta"/> to fall in the
/// <c>-100..100</c> band Alexa documents for the directive.
/// </summary>
/// <remarks>
/// Negative values dim the light, positive values brighten it. Anything
/// outside the band is a spec violation and shouldn't reach the HA service
/// layer.
/// </remarks>
public class AdjustBrightnessPayloadValidator : AbstractValidator<AdjustBrightnessPayload>
{
    /// <summary>
    /// Configures the brightness-delta range rule that <c>Validate</c>
    /// applies to inbound <c>AdjustBrightness</c> payloads.
    /// </summary>
    /// <remarks>
    /// Single rule: <c>brightnessDelta</c> within <c>-100..100</c>.
    /// </remarks>
    public AdjustBrightnessPayloadValidator()
    {
        RuleFor(p => p.BrightnessDelta)
            .InclusiveBetween(-100, 100)
            .WithMessage("BrightnessDelta must be between -100 and 100");
    }
}
