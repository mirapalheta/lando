using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// The endpoint object on an inbound directive: which device the directive targets, plus
/// the OAuth scope Alexa is presenting on behalf of the user.
/// </summary>
public sealed class DirectiveEndpoint
{
    /// <summary>
    /// OAuth scope identifying the customer..
    /// </summary>
    [JsonPropertyName("scope")]
    public Scope? Scope { get; set; }

    /// <summary>
    /// Stable, opaque identifier for the device. Set during Discovery..
    /// </summary>
    [JsonPropertyName("endpointId")]
    public string EndpointId { get; set; } = string.Empty;

    /// <summary>
    /// Optional cookies the bridge set on the endpoint during Discovery. Treat as opaque
    /// per-device state. Note: Alexa imposes a 5KB cap on cookies.
    /// </summary>
    [JsonPropertyName("cookie")]
    public Dictionary<string, string>? Cookie { get; set; }
}
