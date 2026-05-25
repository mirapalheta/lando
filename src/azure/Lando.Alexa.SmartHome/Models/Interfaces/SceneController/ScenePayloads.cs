using System.Text.Json.Serialization;
using Lando.Alexa.SmartHome.Models.ChangeReport;

namespace Lando.Alexa.SmartHome.Models.Interfaces.SceneController;

/// <summary>
/// Payload for <c>Alexa.SceneController.ActivationStarted</c> /
/// <c>Alexa.SceneController.DeactivationStarted</c> events. Reports the cause and the
/// timestamp the activation/deactivation began.
/// </summary>
public sealed class SceneActivationPayload
{
    [JsonPropertyName("cause")]
    public Cause Cause { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}
