using Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

/// <summary>
/// Handles <c>Alexa.BrightnessController.AdjustBrightness</c> by sending a relative
/// <c>brightness_step_pct</c> to HA, which avoids the round-trip of reading the current
/// brightness first.
/// </summary>
public class AdjustBrightnessPayloadTransform : IPayloadTransform<AdjustBrightnessPayload>
{
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, AdjustBrightnessPayload payload)
        => HomeAssistantRequest.AdjustBrightness(entity.EntityId, payload.BrightnessDelta);
}
