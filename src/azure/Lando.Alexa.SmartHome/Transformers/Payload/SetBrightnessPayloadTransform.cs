using Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

/// <summary>
/// Handles <c>Alexa.BrightnessController.SetBrightness</c>. Translates the
/// Alexa 0..100 percent into HA's <c>brightness_pct</c> on <c>light.turn_on</c>.
/// </summary>
/// <remarks>
/// HA accepts brightness as a percent directly when the service is called
/// against the light domain, so no unit conversion is required here.
/// </remarks>
public class SetBrightnessPayloadTransform : IPayloadTransform<SetBrightnessPayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, SetBrightnessPayload payload)
        => HomeAssistantRequest.TurnOn(entity.EntityId, payload.Brightness);
}
