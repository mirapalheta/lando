using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// Alexa Custom Skill response envelope. The bridge only ever returns spoken
/// confirmations (no cards, directives, or session state), so the shape is
/// deliberately minimal.
/// </summary>
public sealed class IntentResponse
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("response")]
    public ResponseBody Response { get; set; } = new();

    /// <summary>
    /// Build a plain-text spoken response, ending the session by default.
    /// </summary>
    public static IntentResponse Speak(string text, bool endSession = true) => new()
    {
        Response = new ResponseBody
        {
            OutputSpeech = new OutputSpeech { Text = text },
            ShouldEndSession = endSession
        }
    };
}
