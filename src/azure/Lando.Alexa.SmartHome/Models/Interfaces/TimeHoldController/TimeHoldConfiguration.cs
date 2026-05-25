using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.TimeHoldController;

/// <summary>
/// Configuration block declared on a discovered <c>Alexa.TimeHoldController</c> capability.
/// </summary>
public sealed class TimeHoldConfiguration
{
    /// <summary>
    /// Whether the device allows the customer to remotely resume a held activity.
    /// </summary>
    [JsonPropertyName("allowRemoteResume")]
    public bool AllowRemoteResume { get; set; }
}
