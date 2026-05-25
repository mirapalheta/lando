using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.PercentageController.SetPercentage</c>. Covers route to
/// <c>cover.set_cover_position</c>, fans route to <c>fan.set_percentage</c>,
/// and any other domain is rejected.
/// </summary>
/// <remarks>
/// Retained for backwards compatibility with Alexa endpoint caches that may
/// still send PercentageController directives against cached endpoints —
/// modern discovery advertises RangeController on shade-like covers and on
/// fans, so this handler should rarely be exercised after a rediscover.
/// </remarks>
public class SetPercentagePayloadTransform : IPayloadTransform<SetPercentagePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, SetPercentagePayload payload)
        => entity.GetDomain() switch
        {
            Domains.Cover => HomeAssistantRequest.SetCoverPosition(entity.EntityId, payload.Percentage),
            Domains.Fan => HomeAssistantRequest.SetPercentage(entity.EntityId, payload.Percentage),
            _ => throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support percentage control")
        };
}
