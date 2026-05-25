using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Endpoint object on an outbound event. Required for device-targeted responses; omitted
/// for skill-targeted responses such as <c>Discover.Response</c> or <c>AcceptGrant.Response</c>.
/// </summary>
/// <remarks>
/// When posting asynchronously to the Alexa event gateway, set <see cref="Scope"/> to the
/// caller's BearerToken; for synchronous Lambda responses leave it null.
/// </remarks>
public sealed class EventEndpoint
{
    [JsonPropertyName("scope")]
    public Scope? Scope { get; set; }

    [JsonPropertyName("endpointId")]
    public string EndpointId { get; set; } = string.Empty;
}
