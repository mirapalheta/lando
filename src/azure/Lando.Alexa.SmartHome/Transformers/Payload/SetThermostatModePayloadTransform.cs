using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using HvacModes = Lando.HomeAssistant.Constants.HvacModes;

/// <summary>
/// Handles <c>Alexa.ThermostatController.SetThermostatMode</c> by translating
/// Alexa's uppercase mode strings (HEAT/COOL/AUTO/OFF/...) into HA's
/// lowercase canonical values and dispatching <c>climate.set_hvac_mode</c>.
/// </summary>
/// <remarks>
/// Alexa's <c>EM_HEAT</c> (emergency heat) doesn't have a direct HA
/// equivalent — we map it to <c>heat</c> as the safest fallback.
/// <c>CUSTOM</c> with a <see cref="ThermostatMode.CustomName"/> is forwarded
/// verbatim, letting unusual integrations (for example boost modes) work as
/// long as the customer's HA entity actually supports the value.
/// </remarks>
public class SetThermostatModePayloadTransform : IPayloadTransform<SetThermostatModePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, SetThermostatModePayload payload)
    {
        var mode = payload.ThermostatMode.Value switch
        {
            ThermostatModes.Off => HvacModes.Off,
            ThermostatModes.Heat => HvacModes.Heat,
            ThermostatModes.EmergencyHeat => HvacModes.Heat,
            ThermostatModes.Cool => HvacModes.Cool,
            ThermostatModes.Auto => HvacModes.HeatCool,
            ThermostatModes.EcoMode => HvacModes.Auto,
            ThermostatModes.Custom => payload.ThermostatMode.CustomName ?? HvacModes.Auto,
            _ => payload.ThermostatMode.Value.ToLowerInvariant()
        };
        return HomeAssistantRequest.SetHvacMode(entity.EntityId, mode);
    }
}
