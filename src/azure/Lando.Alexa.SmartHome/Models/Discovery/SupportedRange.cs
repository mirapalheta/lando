using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Numeric range declared on a RangeController capability.
/// </summary>
public sealed class SupportedRange
{
    /// <summary>
    /// Inclusive minimum value.
    /// </summary>
    [JsonPropertyName("minimumValue")]
    public double MinimumValue { get; set; }

    /// <summary>
    /// Inclusive maximum value.
    /// </summary>
    [JsonPropertyName("maximumValue")]
    public double MaximumValue { get; set; }

    /// <summary>
    /// Step granularity (e.g. <c>1.0</c> for integer values).
    /// </summary>
    [JsonPropertyName("precision")]
    public double Precision { get; set; }
}
