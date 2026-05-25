using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Bands configuration for an EqualizerController.
/// </summary>
public sealed class EqualizerBandsConfig
{
    /// <summary>
    /// Bands the device exposes.
    /// </summary>
    [JsonPropertyName("supported")]
    public List<EqualizerBandName> Supported { get; set; } = new();

    /// <summary>
    /// Numeric range each band supports.
    /// </summary>
    [JsonPropertyName("range")]
    public EqualizerRange Range { get; set; } = new();
}
