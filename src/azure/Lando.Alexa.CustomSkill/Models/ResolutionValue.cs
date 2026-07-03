using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// A canonical resolved slot value (its <c>name</c> and optional <c>id</c>).
/// </summary>
public sealed class ResolutionValue
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
