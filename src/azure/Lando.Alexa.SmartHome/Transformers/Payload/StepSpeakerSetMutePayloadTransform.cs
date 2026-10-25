using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.StepSpeaker;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.StepSpeaker.SetMute</c>. Dispatches
/// <c>media_player.volume_mute</c> with the boolean from the inbound payload,
/// exactly as <see cref="SetMutePayloadTransform"/> does for <c>Alexa.Speaker</c>.
/// </summary>
public class StepSpeakerSetMutePayloadTransform : IPayloadTransform<StepSpeakerSetMutePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, StepSpeakerSetMutePayload payload)
        => entity.GetDomain() switch
        {
            Domains.MediaPlayer => HomeAssistantRequest.SetMute(entity.EntityId, payload.Mute),
            _ => throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support mute control")
        };
}
