using System;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.PercentageController.AdjustPercentage</c>. HA exposes no
/// relative "adjust position" service for covers or fans, so this handler
/// reads the current position, applies the delta, clamps to 0..100, and
/// dispatches the appropriate absolute-set service.
/// </summary>
/// <remarks>
/// Retained even though discovery now advertises RangeController on covers
/// and fans — Alexa may still send PercentageController directives against
/// cached endpoints while the user is in the middle of re-discovering after
/// the capability shape change, so the bridge accepts both forms.
/// </remarks>
public class AdjustPercentagePayloadTransform : IPayloadTransform<AdjustPercentagePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, AdjustPercentagePayload payload)
    {
        // HA exposes the current cover position as `current_position`; fans use `percentage`.
        var current = entity.Attributes.GetInt(EntityAttributes.CurrentPosition)
                   ?? entity.Attributes.GetInt(EntityAttributes.Percentage)
                   ?? 0;
        var next = Math.Clamp(current + payload.PercentageDelta, 0, 100);

        return entity.GetDomain() switch
        {
            Domains.Cover => HomeAssistantRequest.SetCoverPosition(entity.EntityId, next),
            Domains.Fan => HomeAssistantRequest.SetPercentage(entity.EntityId, next),
            _ => throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support percentage control")
        };
    }
}
