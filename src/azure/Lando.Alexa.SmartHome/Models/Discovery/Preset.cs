using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Named preset value within a RangeController's range.
/// </summary>
public sealed class Preset
{
    /// <summary>
    /// The numeric value within <see cref="SupportedRange"/> this preset binds to.
    /// </summary>
    [JsonPropertyName("rangeValue")]
    public double RangeValue { get; set; }

    /// <summary>
    /// Friendly-name resources describing the preset to the user.
    /// </summary>
    [JsonPropertyName("presetResources")]
    public CapabilityResources? PresetResources { get; set; }
}
