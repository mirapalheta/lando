using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

/// <summary>
/// Payload for <c>Alexa.ThermostatController.AdjustTargetTemperature</c>.
/// </summary>
public sealed class AdjustTargetTemperaturePayload
{
    /// <summary>
    /// Relative delta to apply to the current setpoint.
    /// </summary>
    [JsonPropertyName("targetSetpointDelta")]
    public Temperature TargetSetpointDelta { get; set; } = new();
}
