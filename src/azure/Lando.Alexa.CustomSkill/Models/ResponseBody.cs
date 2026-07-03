using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// The <c>response</c> body: spoken output plus whether to end the session.
/// </summary>
public sealed class ResponseBody
{
    [JsonPropertyName("outputSpeech")]
    public OutputSpeech? OutputSpeech { get; set; }

    [JsonPropertyName("shouldEndSession")]
    public bool ShouldEndSession { get; set; } = true;
}
