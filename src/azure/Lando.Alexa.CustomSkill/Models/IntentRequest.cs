using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// Top-level Alexa Custom Skill request envelope. The discriminator is
/// <see cref="RequestEnvelope.Type"/> (<c>LaunchRequest</c>,
/// <c>IntentRequest</c>, <c>SessionEndedRequest</c>).
/// </summary>
public sealed class IntentRequest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("request")]
    public RequestEnvelope Request { get; set; } = new();
}
