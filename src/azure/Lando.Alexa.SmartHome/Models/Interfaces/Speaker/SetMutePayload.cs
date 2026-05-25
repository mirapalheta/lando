using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.Speaker;

/// <summary>
/// Payload for <c>Alexa.Speaker.SetMute</c>.
/// </summary>
public sealed class SetMutePayload
{
    /// <summary>
    /// True mutes the device, false unmutes.
    /// </summary>
    [JsonPropertyName("mute")]
    public bool Mute { get; set; }
}
