using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.SecurityPanelController;

/// <summary>
/// Outbound payload for an <c>Arm.Response</c> event.
/// </summary>
public sealed class ArmResponsePayload
{
    /// <summary>
    /// Resulting state after arm completes (might match request, might be <c>DELAYED</c>).
    /// </summary>
    [JsonPropertyName("armState")]
    public string ArmState { get; set; } = SecurityPanelController.ArmState.ArmedAway;

    /// <summary>
    /// Exit delay before the panel actually arms, in seconds.
    /// </summary>
    [JsonPropertyName("exitDelayInSeconds")]
    public int? ExitDelayInSeconds { get; set; }
}
