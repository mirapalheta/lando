namespace Lando.HomeAssistant.Configuration;

/// <summary>
/// Configuration root for the Home Assistant integration. Holds the connection
/// settings bound from the <see cref="SectionName"/> configuration section.
/// </summary>
public class HomeAssistantConfiguration
{
    /// <summary>
    /// Configuration section name bound to this object (<c>HomeAssistant</c>).
    /// </summary>
    public const string SectionName = "HomeAssistant";

    /// <summary>
    /// HTTP / WebSocket client settings used to reach the HA instance.
    /// </summary>
    public HomeAssistantClientOptions ClientOptions { get; set; } = null!;
}
