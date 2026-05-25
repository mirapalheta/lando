using System.Text.Json.Serialization;
using Lando.Alexa.SmartHome.Models.ChangeReport;

namespace Lando.Alexa.SmartHome.Models.Interfaces.DoorbellEventSource;

/// <summary>
/// Payload for the asynchronous <c>Alexa.DoorbellEventSource.DoorbellPress</c> event posted
/// to the Alexa event gateway when a doorbell is pressed.
/// </summary>
public sealed class DoorbellPressPayload
{
    /// <summary>
    /// Cause of the press; always <c>PHYSICAL_INTERACTION</c> for real button presses..
    /// </summary>
    [JsonPropertyName("cause")]
    public Cause Cause { get; set; } = new() { Type = ChangeCauseType.PhysicalInteraction };

    /// <summary>
    /// ISO-8601 UTC timestamp of the press..
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}
