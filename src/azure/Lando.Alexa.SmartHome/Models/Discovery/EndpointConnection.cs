using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Local-protocol connection metadata for an endpoint. Optional, but recommended for
/// Works-with-Alexa certified devices that support local control (Zigbee, Matter, BLE…).
/// </summary>
public sealed class EndpointConnection
{
    /// <summary>
    /// Connection type: <c>TCP_IP</c>, <c>ZIGBEE</c>, <c>ZWAVE</c>, <c>UNKNOWN</c>..
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = ConnectionType.Unknown;

    [JsonPropertyName("macAddress")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("homeId")]
    public string? HomeId { get; set; }

    [JsonPropertyName("nodeId")]
    public string? NodeId { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
