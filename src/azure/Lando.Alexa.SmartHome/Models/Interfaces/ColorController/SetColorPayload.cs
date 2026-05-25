using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.ColorController;

/// <summary>
/// Payload for <c>Alexa.ColorController.SetColor</c>..
/// </summary>
public sealed class SetColorPayload
{
    [JsonPropertyName("color")]
    public HsbColor Color { get; set; } = new();
}
