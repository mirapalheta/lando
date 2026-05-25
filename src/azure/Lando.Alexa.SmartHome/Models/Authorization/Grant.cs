using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Authorization;

/// <summary>
/// Authorization grant returned by Alexa during enablement. The skill exchanges
/// <see cref="Code"/> for a refresh/access token pair against LWA, then stores those for
/// the customer so the bridge can post to the event gateway on their behalf.
/// </summary>
public sealed class Grant
{
    /// <summary>
    /// Always <c>"OAuth2.AuthorizationCode"</c>..
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = GrantType.OAuth2AuthorizationCode;

    /// <summary>
    /// The authorization code to redeem at the LWA token endpoint..
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}
