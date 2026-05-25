using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.ChangeReport;

/// <summary>
/// Payload for an asynchronously posted <c>Alexa.ChangeReport</c> event. Always paired with
/// a populated <c>context</c> on the outer <c>AlexaResponse</c> that carries the rest of the
/// endpoint's current state.
/// </summary>
public sealed class ChangeReportPayload
{
    [JsonPropertyName("change")]
    public Change Change { get; set; } = new();
}
