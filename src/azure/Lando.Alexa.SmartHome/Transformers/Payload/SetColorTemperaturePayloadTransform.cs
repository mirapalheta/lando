using Lando.Alexa.SmartHome.Models.Interfaces.ColorTemperatureController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

/// <summary>
/// Handles <c>Alexa.ColorTemperatureController.SetColorTemperature</c>. Alexa
/// sends Kelvin directly; HA accepts Kelvin natively, so no unit conversion is
/// required.
/// </summary>
/// <remarks>
/// The Increase/Decrease companion directives don't carry a payload and live
/// in their own stateful handlers; this one is the absolute-set entry point.
/// </remarks>
public class SetColorTemperaturePayloadTransform : IPayloadTransform<SetColorTemperaturePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, SetColorTemperaturePayload payload)
        => HomeAssistantRequest.SetColorTemperature(entity.EntityId, payload.ColorTemperatureInKelvin);
}
