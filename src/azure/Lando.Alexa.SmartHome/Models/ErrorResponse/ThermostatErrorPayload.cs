using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.ErrorResponse;

/// <summary>
/// ThermostatController-specific error variants. The <see cref="ErrorPayload.Type"/> field
/// distinguishes between <c>UNSUPPORTED_THERMOSTAT_MODE</c>, <c>DUAL_SETPOINTS_TOO_CLOSE</c>,
/// <c>REQUESTED_SETPOINT_TOO_HIGH</c>, <c>REQUESTED_SETPOINT_TOO_LOW</c>, etc.
/// </summary>
public sealed class ThermostatErrorPayload : ErrorPayload
{
    /// <summary>
    /// For <c>UNSUPPORTED_THERMOSTAT_MODE</c>: the modes the thermostat does support..
    /// </summary>
    [JsonPropertyName("validModes")]
    public List<string>? ValidModes { get; set; }

    /// <summary>For <c>DUAL_SETPOINTS_TOO_CLOSE</c>: the minimum allowed setpoint delta in the
    /// thermostat's unit.</summary>
    [JsonPropertyName("minimumTemperatureDelta")]
    public object? MinimumTemperatureDelta { get; set; }
}
