using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.ErrorResponse;

/// <summary>
/// SecurityPanelController-specific error variant. Reports current alarm and trouble states
/// so Alexa can read them back to the user.
/// </summary>
public sealed class SecurityPanelErrorPayload : ErrorPayload
{
    [JsonPropertyName("alarms")]
    public Dictionary<string, AlarmStateValue>? Alarms { get; set; }

    [JsonPropertyName("burglaryAlarm")]
    public AlarmStateValue? BurglaryAlarm { get; set; }

    [JsonPropertyName("fireAlarm")]
    public AlarmStateValue? FireAlarm { get; set; }

    [JsonPropertyName("carbonMonoxideAlarm")]
    public AlarmStateValue? CarbonMonoxideAlarm { get; set; }
}

/// <summary>
/// Wrapped alarm state value, e.g. <c>{"value": "ALARM"}</c>..
/// </summary>
public sealed class AlarmStateValue
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
