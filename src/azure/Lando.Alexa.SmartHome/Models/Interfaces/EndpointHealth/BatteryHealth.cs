using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.EndpointHealth;

/// <summary>
/// Battery health sub-property for <c>Alexa.EndpointHealth.battery</c>.
/// All fields are optional.
/// </summary>
public sealed class BatteryHealth
{
    /// <summary>
    /// Wrapped battery health state.
    /// </summary>
    [JsonPropertyName("health")]
    public BatteryHealthState? HealthState { get; set; }

    /// <summary>
    /// Battery level as a percentage (0–100).
    /// </summary>
    [JsonPropertyName("levelInPercentage")]
    public int? LevelInPercentage { get; set; }

    /// <summary>
    /// Wrapped state-of-charge descriptor.
    /// </summary>
    [JsonPropertyName("stateOfCharge")]
    public BatteryStateOfCharge? StateOfCharge { get; set; }
}
