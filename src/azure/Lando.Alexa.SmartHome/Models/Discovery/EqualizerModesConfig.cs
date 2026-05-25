using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Modes configuration for an EqualizerController.
/// </summary>
public sealed class EqualizerModesConfig
{
    /// <summary>
    /// Modes the device exposes.
    /// </summary>
    [JsonPropertyName("supported")]
    public List<EqualizerModeName> Supported { get; set; } = new();
}
