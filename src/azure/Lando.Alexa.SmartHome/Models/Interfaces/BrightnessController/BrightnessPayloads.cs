using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;

/// <summary>
/// Payload for <c>Alexa.BrightnessController.SetBrightness</c>..
/// </summary>
public sealed class SetBrightnessPayload
{
    /// <summary>
    /// Absolute brightness on 0–100..
    /// </summary>
    [JsonPropertyName("brightness")]
    public int Brightness { get; set; }
}

/// <summary>
/// Payload for <c>Alexa.BrightnessController.AdjustBrightness</c>..
/// </summary>
public sealed class AdjustBrightnessPayload
{
    /// <summary>
    /// Delta on -100..100. Negative dims, positive brightens..
    /// </summary>
    [JsonPropertyName("brightnessDelta")]
    public int BrightnessDelta { get; set; }
}

/// <summary>
/// Property names exposed by <c>Alexa.BrightnessController</c>..
/// </summary>
public static class BrightnessControllerProperties
{
    public const string Brightness = "brightness";
}
