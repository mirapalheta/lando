using System.Text.Json.Serialization;

namespace Lando.Security;

/// <summary>
/// JSON shape returned by <c>/auth/o2/tokeninfo</c>. Field names match the
/// LWA wire format; populated by the JSON deserialiser inside
/// <see cref="ITokenClient.GetAsync"/>.
/// </summary>
public sealed class TokenInfo
{
    /// <summary>
    /// Issuer of the token — for LWA, <c>https://www.amazon.com</c>.
    /// </summary>
    [JsonPropertyName("iss")]
    public string? Iss { get; set; }

    /// <summary>
    /// Stable LWA identifier of the Amazon customer this token is bound to.
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// OAuth audience claim — the LWA client_id the token was minted for.
    /// </summary>
    [JsonPropertyName("aud")]
    public string? Aud { get; set; }

    /// <summary>
    /// LWA app identifier corresponding to <see cref="Aud"/>.
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// Remaining lifetime of the token in seconds (not an absolute timestamp).
    /// </summary>
    [JsonPropertyName("exp")]
    public int Exp { get; set; }
}
