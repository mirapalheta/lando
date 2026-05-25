using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Discovery;

/// <summary>
/// Payload of a <c>Discover.Response</c> event. Alexa caps responses at 300 endpoints; the
/// bridge is responsible for chunking larger inventories across multiple discovery cycles.
/// </summary>
public sealed class DiscoveryResponsePayload
{
    [JsonPropertyName("endpoints")]
    public List<DiscoveryEndpoint> Endpoints { get; set; } = new();
}
