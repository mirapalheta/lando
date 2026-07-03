using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// Resolution result for one authority: a status plus any matched values.
/// </summary>
public sealed class ResolutionAuthority
{
    [JsonPropertyName("status")]
    public ResolutionStatus? Status { get; set; }

    [JsonPropertyName("values")]
    public List<ResolutionValueWrapper>? Values { get; set; }
}
