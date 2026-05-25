using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.PowerLevelController;

/// <summary>
/// Payload for <c>Alexa.PowerLevelController.SetPowerLevel</c>.
/// </summary>
public sealed class SetPowerLevelPayload
{
    /// <summary>
    /// Absolute level 0..100.
    /// </summary>
    [JsonPropertyName("powerLevel")]
    public int PowerLevel { get; set; }
}
