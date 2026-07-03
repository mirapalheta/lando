using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// Slot-value resolutions, one block per configured authority (slot type).
/// </summary>
public sealed class Resolutions
{
    [JsonPropertyName("resolutionsPerAuthority")]
    public List<ResolutionAuthority>? PerAuthority { get; set; }
}
