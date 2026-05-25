using System;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.RangeController.SetRangeValue</c>. Covers (shade-style
/// endpoints) route to <c>cover.set_cover_position</c>; fans route to
/// <c>fan.set_percentage</c>. Any other domain is rejected.
/// </summary>
/// <remarks>
/// <para>
/// The bridge currently advertises one RangeController instance per
/// domain — <see cref="Capability.ShadePositionInstance"/> on positionable
/// shade-like covers and <see cref="Capability.FanSpeedInstance"/> on fans
/// with <c>SET_SPEED</c> — so the handler does not need to branch on the
/// directive's instance id today. If tilt or a new fan dimension is added
/// later, this is the place to dispatch on <c>directive.header.instance</c>.
/// </para>
/// <para>
/// Alexa's semantic <c>actionMappings</c> rewrite "open"/"close"/"raise"/
/// "lower" utterances into <c>SetRangeValue</c> directives with payloads of
/// 0 or 100, so the natural verbs all funnel through this handler.
/// </para>
/// </remarks>
public class SetRangeValuePayloadTransform : IPayloadTransform<SetRangeValuePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, SetRangeValuePayload payload)
    {
        // RangeValue is a double; clamp + round to the int the HA services expect.
        var value = (int)Math.Round(Math.Clamp(payload.RangeValue, 0d, 100d));

        return entity.GetDomain() switch
        {
            Domains.Cover => HomeAssistantRequest.SetCoverPosition(entity.EntityId, value),
            Domains.Fan => HomeAssistantRequest.SetPercentage(entity.EntityId, value),
            _ => throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support range control")
        };
    }
}
