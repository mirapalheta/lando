using System;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.StepSpeaker;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.StepSpeaker.AdjustVolume</c>. Dispatches
/// <c>media_player.volume_up</c> (positive steps) or <c>media_player.volume_down</c>
/// (negative steps), repeated once per step — HA's step services take no amount.
/// </summary>
/// <remarks>
/// When the user names no amount ("turn up the TV"), Alexa sets
/// <see cref="StepSpeakerAdjustVolumePayload.VolumeStepsDefault"/> and sends a
/// default of ±10 steps, which on a key-press integration is ten remote presses.
/// Like HA's own <c>alexa</c> integration, the default collapses to a single step
/// in the requested direction; an explicit count ("turn it up by 5") is honoured.
/// </remarks>
public class StepSpeakerAdjustVolumePayloadTransform : IPayloadTransform<StepSpeakerAdjustVolumePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, StepSpeakerAdjustVolumePayload payload)
    {
        if (entity.GetDomain() != Domains.MediaPlayer)
            throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support volume control");

        var steps = payload.VolumeStepsDefault == true ? Math.Sign(payload.VolumeSteps) : payload.VolumeSteps;

        return steps > 0
            ? HomeAssistantRequest.VolumeUp(entity.EntityId, steps)
            : HomeAssistantRequest.VolumeDown(entity.EntityId, -steps);
    }
}
