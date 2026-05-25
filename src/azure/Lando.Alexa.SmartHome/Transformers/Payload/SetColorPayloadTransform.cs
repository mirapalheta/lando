using Lando.Alexa.SmartHome.Models.Interfaces.ColorController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

/// <summary>
/// Handles the <c>Alexa.ColorController.SetColor</c> directive. Translates
/// Alexa's HSB triple (hue 0..360, saturation 0..1, brightness 0..1) into HA's
/// <c>hs_color</c> plus <c>brightness_pct</c> shape on <c>light.turn_on</c>.
/// </summary>
/// <remarks>
/// Alexa never sends Kelvin via this directive — that's the separate
/// <c>SetColorTemperature</c> directive — so the colour-vs-temperature
/// branching is not needed here.
/// </remarks>
public class SetColorPayloadTransform : IPayloadTransform<SetColorPayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, SetColorPayload payload)
        => HomeAssistantRequest.SetLightColor(
            entity.EntityId,
            hue: payload.Color.Hue,
            saturationPercent: payload.Color.Saturation * 100.0,
            brightnessPercent: payload.Color.Brightness * 100.0
        );
}
