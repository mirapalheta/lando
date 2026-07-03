using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// Plain-text speech returned to Alexa.
/// </summary>
public sealed class OutputSpeech
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "PlainText";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
