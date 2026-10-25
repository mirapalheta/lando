using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.StepSpeaker;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates <see cref="StepSpeakerAdjustVolumePayload"/>: requires a non-zero
/// <see cref="StepSpeakerAdjustVolumePayload.VolumeSteps"/> in the <c>-100..100</c>
/// band Alexa documents for the directive.
/// </summary>
/// <remarks>
/// Zero is rejected because it carries no direction — the transform would have
/// nothing to dispatch. The upper bound also caps the number of back-to-back
/// volume_up/volume_down calls one directive can fan out to.
/// </remarks>
public class StepSpeakerAdjustVolumePayloadValidator : AbstractValidator<StepSpeakerAdjustVolumePayload>
{
    /// <summary>
    /// Configures the volume-steps rule applied to inbound
    /// <c>Alexa.StepSpeaker.AdjustVolume</c> payloads.
    /// </summary>
    public StepSpeakerAdjustVolumePayloadValidator()
    {
        RuleFor(p => p.VolumeSteps)
            .NotEqual(0)
            .WithMessage("Volume steps must be non-zero")
            .InclusiveBetween(-100, 100)
            .WithMessage("Volume steps must be between -100 and 100");
    }
}
