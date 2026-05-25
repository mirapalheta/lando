using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Interfaces.WakeOnLANController;

/// <summary>
/// Configuration declared on a discovered <c>Alexa.WakeOnLANController</c> capability.
/// Provides Alexa with the MAC addresses to send magic packets to when waking the device.
/// </summary>
public sealed class WakeOnLANConfiguration
{
    /// <summary>
    /// MAC addresses to send magic packets to.
    /// </summary>
    [JsonPropertyName("MACAddresses")]
    public List<string> MacAddresses { get; set; } = new();
}
