using System.Text.Json.Serialization;
using Lando.Alexa.SmartHome.Models.Core;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Payload for an inbound <c>Alexa.Discovery.Discover</c> directive. The only field of
/// interest is the OAuth scope identifying the customer whose endpoints to enumerate.
/// </summary>
public sealed class DiscoveryDirectivePayload
{
    [JsonPropertyName("scope")]
    public Scope? Scope { get; set; }
}
