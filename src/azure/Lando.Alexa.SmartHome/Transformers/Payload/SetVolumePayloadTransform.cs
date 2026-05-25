using System;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.Speaker.SetVolume</c>. HA models volume as a 0.0..1.0
/// float, so the 0..100 integer from Alexa is divided down before
/// dispatching <c>media_player.volume_set</c>.
/// </summary>
/// <remarks>
/// Non-media_player domains are rejected — <c>Alexa.Speaker</c> is only
/// advertised on media players, so any other entity reaching this handler
/// is a misconfiguration on Alexa's side.
/// </remarks>
public class SetVolumePayloadTransform : IPayloadTransform<SetVolumePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, SetVolumePayload payload)
        => entity.GetDomain() switch
        {
            Domains.MediaPlayer => HomeAssistantRequest.SetVolume(entity.EntityId, Math.Clamp(payload.Volume, 0, 100) / 100.0),
            _ => throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support volume control")
        };
}
