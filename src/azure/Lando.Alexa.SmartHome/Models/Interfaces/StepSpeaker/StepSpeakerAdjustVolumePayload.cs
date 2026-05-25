using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.StepSpeaker;

/// <summary>
/// Payload for <c>Alexa.StepSpeaker.AdjustVolume</c> — relative steps only.
/// </summary>
public sealed class StepSpeakerAdjustVolumePayload
{
    /// <summary>
    /// Number of volume steps. Sign indicates direction.
    /// </summary>
    [JsonPropertyName("volumeSteps")]
    public int VolumeSteps { get; set; }

    /// <summary>
    /// If true, <see cref="VolumeSteps"/> is treated as a default step size hint.
    /// </summary>
    [JsonPropertyName("volumeStepsDefault")]
    public bool? VolumeStepsDefault { get; set; }
}
