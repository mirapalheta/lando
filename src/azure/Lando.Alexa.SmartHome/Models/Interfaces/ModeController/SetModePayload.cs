using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ModeController;

/// <summary>
/// Payload for <c>Alexa.ModeController.SetMode</c>.
/// </summary>
public sealed class SetModePayload
{
    /// <summary>
    /// One of the supported modes declared during Discovery (e.g. <c>"Position.Up"</c>).
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;
}
