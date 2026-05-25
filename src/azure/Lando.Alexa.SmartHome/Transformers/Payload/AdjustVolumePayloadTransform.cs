using System;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.Speaker.AdjustVolume</c>. HA has no relative volume
/// service for media players, so this handler reads the current
/// <c>volume_level</c>, applies the delta, clamps to 0..1, and dispatches
/// <c>media_player.volume_set</c>.
/// </summary>
/// <remarks>
/// Alexa sends the delta as a 0..100 integer; HA volume is a 0.0..1.0
/// float. The payload's <see cref="AdjustVolumePayload.VolumeDefault"/>
/// flag distinguishes "step by default amount" from an explicit delta —
/// both routes funnel into the same service call here since the underlying
/// HA API is identical.
/// </remarks>
public class AdjustVolumePayloadTransform : IPayloadTransform<AdjustVolumePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, AdjustVolumePayload payload)
    {
        if (entity.GetDomain() != Domains.MediaPlayer)
            throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support volume control");

        var current = entity.Attributes.GetDouble(EntityAttributes.VolumeLevel) ?? 0d;
        var next = Math.Clamp(current + payload.Volume / 100.0, 0d, 1d);

        return HomeAssistantRequest.SetVolume(entity.EntityId, next);
    }
}
