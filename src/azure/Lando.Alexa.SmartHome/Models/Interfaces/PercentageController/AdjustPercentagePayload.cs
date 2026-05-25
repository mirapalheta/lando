using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;

/// <summary>
/// Payload for <c>Alexa.PercentageController.AdjustPercentage</c>.
/// </summary>
public sealed class AdjustPercentagePayload
{
    /// <summary>
    /// Delta on -100..100.
    /// </summary>
    [JsonPropertyName("percentageDelta")]
    public int PercentageDelta { get; set; }
}
