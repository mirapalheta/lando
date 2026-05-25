namespace Lando.HomeAssistant.Configuration;

/// <summary>
/// HTTP client configuration options for connecting to Home Assistant.
/// </summary>
public class HomeAssistantClientOptions
{
    /// <summary>
    /// The base URL of the Home Assistant instance (e.g., "https://homeassistant.example.local:8123").
    /// Required. Must be a valid absolute URI.
    /// </summary>
    public string BaseUrl { get; set; } = null!;

    /// <summary>
    /// The long-lived API token for authenticating with Home Assistant.
    /// Required. Used in the Authorization header for all API requests.
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// Optional HTTP proxy address (e.g., "socks5://localhost:1055") for routing requests through Tailscale or other proxies.
    /// If null or empty, no proxy is used.
    /// </summary>
    public string? ProxyAddress { get; set; }

    /// <summary>
    /// Optional URL for health check proxy connectivity.
    /// If configured, the health check will verify the proxy is reachable at this URL.
    /// </summary>
    public string? ProxyHealthCheckUrl { get; set; }

    /// <summary>
    /// Optional base64-encoded custom CA certificate for HTTPS validation.
    /// If configured, this certificate will be used to validate the Home Assistant server's TLS certificate.
    /// Useful for self-signed certificates or internal PKI.
    /// </summary>
    public string? Certificate { get; set; }
}
