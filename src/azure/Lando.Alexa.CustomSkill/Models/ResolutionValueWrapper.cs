using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// Alexa wraps each resolved value in a <c>value</c> object; this is that wrapper.
/// </summary>
public sealed class ResolutionValueWrapper
{
    [JsonPropertyName("value")]
    public ResolutionValue? Value { get; set; }
}
