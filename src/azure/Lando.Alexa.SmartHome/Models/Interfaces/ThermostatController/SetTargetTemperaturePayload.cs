using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

/// <summary>
/// Payload for <c>Alexa.ThermostatController.SetTargetTemperature</c>. Alexa sends exactly
/// one of (<see cref="TargetSetpoint"/>) or (<see cref="LowerSetpoint"/> + <see cref="UpperSetpoint"/>)
/// depending on whether the thermostat is in single- or dual-setpoint mode.
/// </summary>
public sealed class SetTargetTemperaturePayload
{
    /// <summary>
    /// Single-setpoint target. Mutually exclusive with the lower/upper pair.
    /// </summary>
    [JsonPropertyName("targetSetpoint")]
    public Temperature? TargetSetpoint { get; set; }

    /// <summary>
    /// Lower bound for dual-setpoint mode.
    /// </summary>
    [JsonPropertyName("lowerSetpoint")]
    public Temperature? LowerSetpoint { get; set; }

    /// <summary>
    /// Upper bound for dual-setpoint mode.
    /// </summary>
    [JsonPropertyName("upperSetpoint")]
    public Temperature? UpperSetpoint { get; set; }
}
