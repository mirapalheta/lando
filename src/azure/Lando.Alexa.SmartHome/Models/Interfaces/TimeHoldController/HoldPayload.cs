using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.TimeHoldController;

/// <summary>
/// Payload for <c>Alexa.TimeHoldController.Hold</c>.
/// </summary>
public sealed class HoldPayload
{
    /// <summary>
    /// ISO-8601 duration for the hold (e.g. <c>"PT30M"</c>).
    /// </summary>
    [JsonPropertyName("holdDuration")]
    public string? HoldDuration { get; set; }

    /// <summary>
    /// True to extend an existing hold rather than start a new one.
    /// </summary>
    [JsonPropertyName("extendHold")]
    public bool? ExtendHold { get; set; }
}
