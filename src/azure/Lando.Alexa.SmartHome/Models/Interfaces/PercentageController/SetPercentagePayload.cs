using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;

/// <summary>
/// Payload for <c>Alexa.PercentageController.SetPercentage</c>.
/// </summary>
public sealed class SetPercentagePayload
{
    /// <summary>
    /// Absolute percentage 0..100.
    /// </summary>
    [JsonPropertyName("percentage")]
    public int Percentage { get; set; }
}
