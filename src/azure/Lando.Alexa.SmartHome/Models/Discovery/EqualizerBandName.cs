using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Wrapper for a band name in a discovery-time equalizer bands configuration
/// (e.g. <c>"BASS"</c>, <c>"MIDRANGE"</c>, <c>"TREBLE"</c>).
/// </summary>
public sealed class EqualizerBandName
{
    /// <summary>
    /// The band name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
