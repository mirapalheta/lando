using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.SecurityPanelController;

/// <summary>
/// Payload for <c>Alexa.SecurityPanelController.Disarm</c>.
/// </summary>
public sealed class DisarmPayload
{
    /// <summary>
    /// User-supplied PIN to disarm. Optional if PIN isn't required.
    /// </summary>
    [JsonPropertyName("authorization")]
    public Authorization? Authorization { get; set; }
}
