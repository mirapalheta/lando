using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.SecurityPanelController;

/// <summary>
/// Payload for <c>Alexa.SecurityPanelController.Arm</c>.
/// </summary>
public sealed class ArmPayload
{
    /// <summary>
    /// Target arm state — one of <see cref="SecurityPanelController.ArmState"/>'s constants.
    /// </summary>
    [JsonPropertyName("armState")]
    public string ArmState { get; set; } = SecurityPanelController.ArmState.ArmedAway;
}
