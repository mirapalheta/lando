using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.RangeController;

/// <summary>
/// Payload for <c>Alexa.RangeController.SetRangeValue</c>.
/// </summary>
public sealed class SetRangeValuePayload
{
    /// <summary>
    /// Absolute target value within the capability's supported range.
    /// </summary>
    [JsonPropertyName("rangeValue")]
    public double RangeValue { get; set; }
}
