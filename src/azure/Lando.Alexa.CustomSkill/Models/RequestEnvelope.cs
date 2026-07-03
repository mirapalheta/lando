using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// The inner <c>request</c> object of an Alexa Custom Skill envelope.
/// <see cref="Intent"/> is present only for <c>IntentRequest</c>s.
/// </summary>
public sealed class RequestEnvelope
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("intent")]
    public Intent? Intent { get; set; }
}
