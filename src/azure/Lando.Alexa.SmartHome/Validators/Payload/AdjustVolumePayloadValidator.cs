using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates <see cref="AdjustVolumePayload"/>: requires the
/// <see cref="AdjustVolumePayload.Volume"/> delta to fall in the
/// <c>-100..100</c> band Alexa documents for the directive.
/// </summary>
/// <remarks>
/// The payload's <c>volumeDefault</c> flag is a hint about whether the
/// caller wanted a "default step" rather than an explicit value; the bridge
/// treats both the same way so the flag is not subject to validation.
/// </remarks>
public class AdjustVolumePayloadValidator : AbstractValidator<AdjustVolumePayload>
{
    /// <summary>
    /// Configures the volume-delta rule that <c>Validate</c>
    /// applies to inbound <c>AdjustVolume</c> payloads.
    /// </summary>
    /// <remarks>
    /// Single rule: <c>volume</c> (treated as a delta) within
    /// <c>-100..100</c>.
    /// </remarks>
    public AdjustVolumePayloadValidator()
    {
        RuleFor(p => p.Volume)
            .InclusiveBetween(-100, 100)
            .WithMessage("Volume delta must be between -100 and 100");
    }
}
