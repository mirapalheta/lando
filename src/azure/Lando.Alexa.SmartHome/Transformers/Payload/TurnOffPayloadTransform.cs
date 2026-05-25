using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles the <c>Alexa.PowerController.TurnOff</c> directive. Routes covers
/// to <c>cover.close_cover</c> and every other domain to <c>turn_off</c>.
/// </summary>
/// <remarks>
/// Symmetric with <see cref="TurnOnPayloadTransform"/>; the cover branch
/// targets the canonical <c>close_cover</c> service rather than a generic
/// <c>turn_off</c> that not every cover integration implements.
/// </remarks>
public class TurnOffPayloadTransform : IPayloadTransform<EmptyPayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, EmptyPayload payload)
        => entity.GetDomain() switch
        {
            Domains.Cover => HomeAssistantRequest.CloseCover(entity.EntityId),
            _ => HomeAssistantRequest.TurnOff(entity.EntityId)
        };
}
