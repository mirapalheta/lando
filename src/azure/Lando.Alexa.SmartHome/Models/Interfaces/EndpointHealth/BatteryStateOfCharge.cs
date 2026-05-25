using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.EndpointHealth;

/// <summary>
/// Wrapped battery state-of-charge.
/// </summary>
public sealed class BatteryStateOfCharge
{
    /// <summary>
    /// Charge state (e.g. <c>"OK"</c>, <c>"LOW"</c>).
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = "OK";
}
