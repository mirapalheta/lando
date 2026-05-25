using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.Speaker.SetMute</c>. Dispatches
/// <c>media_player.volume_mute</c> with the boolean from the inbound
/// payload — HA's service is symmetric, accepting both mute and unmute
/// through the same call.
/// </summary>
/// <remarks>
/// Non-media_player domains are rejected for the same reason as
/// <see cref="SetVolumePayloadTransform"/>: <c>Alexa.Speaker</c> is only
/// advertised on media players.
/// </remarks>
public class SetMutePayloadTransform : IPayloadTransform<SetMutePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, SetMutePayload payload)
        => entity.GetDomain() switch
        {
            Domains.MediaPlayer => HomeAssistantRequest.SetMute(entity.EntityId, payload.Mute),
            _ => throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support mute control")
        };
}
