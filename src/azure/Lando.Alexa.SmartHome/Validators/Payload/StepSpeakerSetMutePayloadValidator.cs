using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.StepSpeaker;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validator for <see cref="StepSpeakerSetMutePayload"/>. No rules — the single
/// boolean field is valid either way — but registered so the directive handler's
/// <c>IValidator&lt;StepSpeakerSetMutePayload&gt;</c> dependency resolves, as with
/// <see cref="SetMutePayloadValidator"/>.
/// </summary>
public class StepSpeakerSetMutePayloadValidator : AbstractValidator<StepSpeakerSetMutePayload>
{
}
