using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles the <c>Alexa.PowerController.TurnOn</c> directive. Routes covers to
/// <c>cover.open_cover</c> and everything else to the domain's <c>turn_on</c>
/// service through Home Assistant.
/// </summary>
/// <remarks>
/// Covers are an exception because HA's <c>cover.turn_on</c> service does not
/// exist on every cover integration — using <c>cover.open_cover</c> hits the
/// canonical service that every cover supports.
/// </remarks>
public class TurnOnPayloadTransform : IPayloadTransform<EmptyPayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, EmptyPayload payload)
        => entity.GetDomain() switch
        {
            Domains.Cover => HomeAssistantRequest.OpenCover(entity.EntityId),
            _ => HomeAssistantRequest.TurnOn(entity.EntityId)
        };
}
