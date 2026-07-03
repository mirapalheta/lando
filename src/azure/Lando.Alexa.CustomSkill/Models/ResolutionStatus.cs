using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// Resolution status; <c>ER_SUCCESS_MATCH</c> indicates a canonical match was found.
/// </summary>
public sealed class ResolutionStatus
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}
