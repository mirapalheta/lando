using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.EndpointHealth;

/// <summary>
/// Value object reported for the <c>Alexa.EndpointHealth.connectivity</c> property.
/// The bridge sets <see cref="Value"/> based on whether the upstream device is reachable.
/// </summary>
public sealed class Connectivity
{
    /// <summary>
    /// Connectivity state — one of <see cref="ConnectivityValue"/>.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = ConnectivityValue.Ok;
}
