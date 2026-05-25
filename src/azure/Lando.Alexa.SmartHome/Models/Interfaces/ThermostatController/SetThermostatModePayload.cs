using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

/// <summary>
/// Payload for <c>Alexa.ThermostatController.SetThermostatMode</c>.
/// </summary>
public sealed class SetThermostatModePayload
{
    /// <summary>
    /// Target mode (canonical or <c>CUSTOM</c> with a name).
    /// </summary>
    [JsonPropertyName("thermostatMode")]
    public ThermostatMode ThermostatMode { get; set; } = new();
}
