using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

/// <summary>
/// Thermostat mode value object. The mode can be a known canonical value
/// (<see cref="ThermostatModes"/>) or a custom string when paired with
/// <see cref="CustomName"/>.
/// </summary>
public sealed class ThermostatMode
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = ThermostatModes.Auto;

    /// <summary>
    /// Set when <see cref="Value"/> is <c>CUSTOM</c>..
    /// </summary>
    [JsonPropertyName("customName")]
    public string? CustomName { get; set; }
}
