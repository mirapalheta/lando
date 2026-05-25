using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.RangeController;

/// <summary>
/// Payload for <c>Alexa.RangeController.AdjustRangeValue</c>.
/// </summary>
public sealed class AdjustRangeValuePayload
{
    /// <summary>
    /// Relative delta to apply to the current range value.
    /// </summary>
    [JsonPropertyName("rangeValueDelta")]
    public double RangeValueDelta { get; set; }

    /// <summary>
    /// If true the delta is treated as a default step rather than an exact value.
    /// </summary>
    [JsonPropertyName("rangeValueDeltaDefault")]
    public bool? RangeValueDeltaDefault { get; set; }
}
