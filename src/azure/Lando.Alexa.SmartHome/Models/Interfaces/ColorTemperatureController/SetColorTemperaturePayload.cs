using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ColorTemperatureController;

/// <summary>
/// Payload for <c>Alexa.ColorTemperatureController.SetColorTemperature</c>.
/// </summary>
public sealed class SetColorTemperaturePayload
{
    /// <summary>
    /// Color temperature in Kelvin, typically 1000–10000.
    /// </summary>
    [JsonPropertyName("colorTemperatureInKelvin")]
    public int ColorTemperatureInKelvin { get; set; }
}
