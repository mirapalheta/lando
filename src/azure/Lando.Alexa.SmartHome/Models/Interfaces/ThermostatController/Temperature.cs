using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

/// <summary>
/// Reusable temperature value object used by <c>Alexa.ThermostatController</c> and
/// <c>Alexa.TemperatureSensor</c>. Always pair a numeric value with the scale to avoid
/// Fahrenheit/Celsius mix-ups at the boundary.
/// </summary>
public sealed class Temperature
{
    [JsonPropertyName("value")]
    public double Value { get; set; }

    /// <summary>
    /// One of <see cref="TemperatureScale"/>'s constants..
    /// </summary>
    [JsonPropertyName("scale")]
    public string Scale { get; set; } = TemperatureScale.Celsius;
}
