namespace Lando.Security;

/// <summary>
/// OAuth2 client credentials used when calling token endpoints.
/// </summary>
public class ClientCredentials
{
    /// <summary>
    /// OAuth2 client identifier.
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// OAuth2 client secret.
    /// </summary>
    public string ClientSecret { get; set; } = null!;
}
