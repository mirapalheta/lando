using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Numeric range a band supports.
/// </summary>
public sealed class EqualizerRange
{
    /// <summary>
    /// Minimum supported level.
    /// </summary>
    [JsonPropertyName("minimum")]
    public int Minimum { get; set; }

    /// <summary>
    /// Maximum supported level.
    /// </summary>
    [JsonPropertyName("maximum")]
    public int Maximum { get; set; }
}
