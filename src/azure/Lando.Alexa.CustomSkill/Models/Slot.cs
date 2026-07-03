using System.Text.Json.Serialization;

namespace Lando.Alexa.CustomSkill.Models;

/// <summary>
/// A single filled slot. <see cref="Value"/> is the spoken/normalized value;
/// <see cref="Resolutions"/> carries the canonical match for custom slot types.
/// </summary>
public sealed class Slot
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("resolutions")]
    public Resolutions? Resolutions { get; set; }
}
