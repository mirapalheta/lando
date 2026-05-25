using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.PowerLevelController;

/// <summary>
/// Payload for <c>Alexa.PowerLevelController.AdjustPowerLevel</c>.
/// </summary>
public sealed class AdjustPowerLevelPayload
{
    /// <summary>
    /// Relative delta on -100..100.
    /// </summary>
    [JsonPropertyName("powerLevelDelta")]
    public int PowerLevelDelta { get; set; }
}
