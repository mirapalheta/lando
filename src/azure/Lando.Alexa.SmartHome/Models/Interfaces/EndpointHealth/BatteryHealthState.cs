using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.EndpointHealth;

/// <summary>
/// Wrapped battery health state.
/// </summary>
public sealed class BatteryHealthState
{
    /// <summary>
    /// State string (e.g. <c>"OK"</c>, <c>"LOW"</c>, <c>"CRITICAL"</c>).
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = "OK";
}
