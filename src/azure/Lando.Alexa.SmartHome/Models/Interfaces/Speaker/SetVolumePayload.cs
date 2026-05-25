using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.Speaker;

/// <summary>
/// Payload for <c>Alexa.Speaker.SetVolume</c>.
/// </summary>
public sealed class SetVolumePayload
{
    /// <summary>
    /// Absolute volume 0..100.
    /// </summary>
    [JsonPropertyName("volume")]
    public int Volume { get; set; }
}
