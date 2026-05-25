using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Wrapper for an equalizer mode name (e.g. <c>"MOVIE"</c>, <c>"MUSIC"</c>,
/// <c>"NIGHT"</c>, <c>"SPORT"</c>, <c>"TV"</c>).
/// </summary>
public sealed class EqualizerModeName
{
    /// <summary>
    /// The mode name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
