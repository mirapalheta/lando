namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Known values for <see cref="EndpointConnection.Type"/>.
/// </summary>
public static class ConnectionType
{
    public const string TcpIp = "TCP_IP";
    public const string Zigbee = "ZIGBEE";
    public const string ZWave = "ZWAVE";
    public const string Unknown = "UNKNOWN";
}
