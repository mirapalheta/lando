using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Authorization;

/// <summary>
/// Information about the user granting authorization to the skill.
/// </summary>
public sealed class Grantee
{
    /// <summary>
    /// Always <c>"BearerToken"</c>..
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "BearerToken";

    /// <summary>
    /// The customer's existing access token within Alexa..
    /// </summary>
    [JsonPropertyName("token")]
    public SecureString Token { get; set; } = new(default);
}
