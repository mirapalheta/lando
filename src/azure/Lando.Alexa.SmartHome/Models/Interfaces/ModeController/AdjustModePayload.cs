using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ModeController;

/// <summary>
/// Payload for <c>Alexa.ModeController.AdjustMode</c>. Only valid on ordered modes.
/// </summary>
public sealed class AdjustModePayload
{
    /// <summary>
    /// Number of positions to move (positive forward, negative backward).
    /// </summary>
    [JsonPropertyName("modeDelta")]
    public int ModeDelta { get; set; }
}
