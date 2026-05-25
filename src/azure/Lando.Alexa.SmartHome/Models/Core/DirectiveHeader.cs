using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Header that identifies a Smart Home directive: which interface, which directive, and the
/// correlation ids that need to be mirrored back on the response.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Instance"/> property is set for interfaces that can be instantiated multiple
/// times on a single endpoint (<c>Alexa.ModeController</c>, <c>Alexa.RangeController</c>,
/// <c>Alexa.ToggleController</c>); it disambiguates which instance the directive targets.
/// </para>
/// </remarks>
public sealed class DirectiveHeader
{
    /// <summary>
    /// The interface namespace, e.g. <c>"Alexa.PowerController"</c>..
    /// </summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// The directive name within the namespace, e.g. <c>"TurnOn"</c>..
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier — v4 UUID recommended — provided by Alexa..
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Opaque token used to correlate directives and responses. Echo it back unchanged..
    /// </summary>
    [JsonPropertyName("correlationToken")]
    public SecureString CorrelationToken { get; set; }

    /// <summary>
    /// Payload schema version. Always <c>"3"</c> for the modern Smart Home API..
    /// </summary>
    [JsonPropertyName("payloadVersion")]
    public string PayloadVersion { get; set; } = Core.PayloadVersion.V3;

    /// <summary>
    /// Instance identifier for multi-instance interfaces such as <c>Alexa.RangeController</c>.
    /// Null on single-instance interfaces.
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }
}
