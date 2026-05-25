using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles <c>Alexa.ThermostatController.AdjustTargetTemperature</c>. HA's
/// climate domain has no relative-adjust service, so this handler reads the
/// current setpoint, applies the delta, and dispatches
/// <c>climate.set_temperature</c>.
/// </summary>
/// <remarks>
/// The HA attribute that holds the current setpoint is <c>temperature</c>
/// for single-mode thermostats. Range-mode thermostats use
/// <c>target_temp_low</c>/<c>target_temp_high</c>; for those, Alexa sends a
/// separate <c>SetTargetTemperature</c> directive with both setpoints
/// rather than an adjust, so the range-mode branch is not needed here.
/// </remarks>
public class AdjustTargetTemperaturePayloadTransform : IPayloadTransform<AdjustTargetTemperaturePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, AdjustTargetTemperaturePayload payload)
    {
        var current = entity.Attributes.GetDouble(EntityAttributes.Temperature)
            ?? throw new AlexaSmartHomeException(ErrorType.InvalidValue, "Thermostat does not currently report a target temperature");

        var next = current + payload.TargetSetpointDelta.Value;
        return HomeAssistantRequest.SetTemperature(entity.EntityId, next);
    }
}
