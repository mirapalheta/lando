using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// The inner directive object. Carried inside <see cref="Request"/>.
/// </summary>
/// <remarks>
/// Discovery / AcceptGrant directives don't include an <see cref="Endpoint"/> — those
/// directives target the skill itself rather than a specific device.
/// </remarks>
public sealed class Directive
{
    /// <summary>
    /// Interface and directive metadata..
    /// </summary>
    [JsonPropertyName("header")]
    public DirectiveHeader Header { get; set; } = new();

    /// <summary>
    /// Target endpoint. Null for skill-targeted directives (Discovery, AcceptGrant)..
    /// </summary>
    [JsonPropertyName("endpoint")]
    public DirectiveEndpoint? Endpoint { get; set; }

    /// <summary>
    /// The directive-specific payload, kept as a <see cref="JsonElement"/> so callers can
    /// deserialize into the concrete payload type for the interface/directive pair without
    /// the envelope needing to know every possible shape.
    /// </summary>
    [JsonPropertyName("payload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public JsonElement? Payload { get; set; }
}
