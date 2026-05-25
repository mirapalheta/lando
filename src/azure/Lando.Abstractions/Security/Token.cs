using System.Text.Json.Serialization;

namespace Lando.Security;

/// <summary>
/// OAuth2 token response. The shape matches what RFC 6749 expects from a token endpoint,
/// with field names mapped to the wire-level snake_case via <see cref="JsonPropertyNameAttribute"/>.
/// </summary>
public sealed class Token
{
    /// <summary>
    /// The bearer token to present on subsequent authenticated requests.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;

    /// <summary>
    /// Only present on initial code-exchange responses; may be omitted on refresh.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token type — almost always <c>"bearer"</c>.
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "bearer";

    /// <summary>
    /// Seconds until <see cref="AccessToken"/> expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
