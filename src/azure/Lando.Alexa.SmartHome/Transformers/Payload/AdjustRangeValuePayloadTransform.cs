using System;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.RangeController.AdjustRangeValue</c>. HA has no relative
/// "adjust" service on covers or fans, so this handler reads the current
/// position, applies the delta, clamps to 0..100, and dispatches the
/// absolute-set service.
/// </summary>
/// <remarks>
/// Mirrors <see cref="AdjustPercentagePayloadTransform"/>. When
/// <see cref="AdjustRangeValuePayload.RangeValueDeltaDefault"/> is true the
/// payload is a "default step" — we treat it the same as an exact step
/// because the bridge advertises a precision of 1 over a 0..100 range,
/// which makes any nominal step equal to its numeric value.
/// </remarks>
public class AdjustRangeValuePayloadTransform : IPayloadTransform<AdjustRangeValuePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, AdjustRangeValuePayload payload)
    {
        var current = entity.Attributes.GetInt(EntityAttributes.CurrentPosition)
                   ?? entity.Attributes.GetInt(EntityAttributes.Percentage)
                   ?? 0;
        var next = (int)Math.Round(Math.Clamp(current + payload.RangeValueDelta, 0d, 100d));

        return entity.GetDomain() switch
        {
            Domains.Cover => HomeAssistantRequest.SetCoverPosition(entity.EntityId, next),
            Domains.Fan => HomeAssistantRequest.SetPercentage(entity.EntityId, next),
            _ => throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support range control")
        };
    }
}
