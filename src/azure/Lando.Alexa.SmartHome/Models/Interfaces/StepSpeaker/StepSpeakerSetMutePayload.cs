using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.StepSpeaker;

/// <summary>
/// Payload for <c>Alexa.StepSpeaker.SetMute</c>.
/// </summary>
public sealed class StepSpeakerSetMutePayload
{
    /// <summary>
    /// True mutes the device, false unmutes.
    /// </summary>
    [JsonPropertyName("mute")]
    public bool Mute { get; set; }
}
