using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.Speaker;

/// <summary>
/// Payload for <c>Alexa.Speaker.AdjustVolume</c>.
/// </summary>
public sealed class AdjustVolumePayload
{
    /// <summary>
    /// Relative volume delta (or absolute default value when <see cref="VolumeDefault"/> is true).
    /// </summary>
    [JsonPropertyName("volume")]
    public int Volume { get; set; }

    /// <summary>
    /// If true, <see cref="Volume"/> uses the device's default instead of a delta.
    /// </summary>
    [JsonPropertyName("volumeDefault")]
    public bool? VolumeDefault { get; set; }
}
