using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ColorController;

/// <summary>
/// HSB color value for <c>Alexa.ColorController.color</c>. Alexa always uses HSB on the wire
/// — convert to/from RGB or CIE xy at the device-cloud boundary.
/// </summary>
public sealed class HsbColor
{
    /// <summary>
    /// Hue in degrees, 0–360..
    /// </summary>
    [JsonPropertyName("hue")]
    public double Hue { get; set; }

    /// <summary>
    /// Saturation 0..1..
    /// </summary>
    [JsonPropertyName("saturation")]
    public double Saturation { get; set; }

    /// <summary>
    /// Brightness 0..1. Note: distinct from BrightnessController.brightness (0–100)..
    /// </summary>
    [JsonPropertyName("brightness")]
    public double Brightness { get; set; }
}
