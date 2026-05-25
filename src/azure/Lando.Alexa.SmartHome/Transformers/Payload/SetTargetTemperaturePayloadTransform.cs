using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

/// <summary>
/// Handles <c>Alexa.ThermostatController.SetTargetTemperature</c>. Alexa
/// sends either a single setpoint (heating-only or cooling-only) or both a
/// lower and upper setpoint (auto/range mode). HA's
/// <c>climate.set_temperature</c> service accepts both shapes, so the
/// handler forwards whichever is populated.
/// </summary>
/// <remarks>
/// The Alexa <see cref="Temperature"/> object carries an explicit scale; we
/// forward the numeric value as-is and rely on the HA climate entity being
/// configured for the same unit. A future improvement would be to convert
/// to the entity's <c>unit_of_measurement</c> here, but most HA
/// installations are consistent.
/// </remarks>
public class SetTargetTemperaturePayloadTransform : IPayloadTransform<SetTargetTemperaturePayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, SetTargetTemperaturePayload payload)
    {
        if (payload.LowerSetpoint is { } low && payload.UpperSetpoint is { } high)
            return HomeAssistantRequest.SetTemperature(entity.EntityId, low.Value, high.Value);

        var temperature = payload.TargetSetpoint?.Value
                  ?? payload.LowerSetpoint?.Value
                  ?? payload.UpperSetpoint?.Value
                  ?? throw new AlexaSmartHomeException(ErrorType.InvalidValue, "No valid setpoint provided");
        return HomeAssistantRequest.SetTemperature(entity.EntityId, temperature);
    }
}
