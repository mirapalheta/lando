using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Authorization;

/// <summary>
/// Inbound payload of an <c>Alexa.Authorization.AcceptGrant</c> directive. Sent once when
/// the customer first enables the skill.
/// </summary>
public sealed class AcceptGrantPayload
{
    [JsonPropertyName("grant")]
    public Grant Grant { get; set; } = new();

    [JsonPropertyName("grantee")]
    public Grantee Grantee { get; set; } = new();
}
