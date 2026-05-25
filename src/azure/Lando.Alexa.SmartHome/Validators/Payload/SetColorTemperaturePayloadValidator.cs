using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorTemperatureController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates <see cref="SetColorTemperaturePayload"/>: requires the
/// <see cref="SetColorTemperaturePayload.ColorTemperatureInKelvin"/> to be
/// in the <c>1000..10000</c> band Alexa documents.
/// </summary>
/// <remarks>
/// 1000K is roughly candlelight; 10000K is cold/blue daylight. Anything
/// outside that band is physically nonsensical for indoor lighting and is
/// either a spec violation or a programming error.
/// </remarks>
public class SetColorTemperaturePayloadValidator : AbstractValidator<SetColorTemperaturePayload>
{
    /// <summary>
    /// Configures the kelvin-range rule that <c>Validate</c>
    /// applies to inbound <c>SetColorTemperature</c> payloads.
    /// </summary>
    /// <remarks>
    /// Single rule: <c>colorTemperatureInKelvin</c> within <c>1000..10000</c>.
    /// </remarks>
    public SetColorTemperaturePayloadValidator()
    {
        RuleFor(p => p.ColorTemperatureInKelvin)
            .InclusiveBetween(1000, 10000)
            .WithMessage("ColorTemperatureInKelvin must be between 1000 and 10000");
    }
}
