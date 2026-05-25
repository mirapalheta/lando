using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Header for an outbound event or synchronous response. Mirrors <see cref="DirectiveHeader"/>
/// but lives on the response side.
/// </summary>
/// <remarks>
/// <see cref="CorrelationToken"/> MUST be copied verbatim from the inbound directive header
/// for the response to be paired correctly by Alexa.
/// </remarks>
public sealed class EventHeader
{
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("correlationToken")]
    public SecureString CorrelationToken { get; set; }

    [JsonPropertyName("payloadVersion")]
    public string PayloadVersion { get; set; } = Core.PayloadVersion.V3;

    /// <summary>
    /// Multi-instance interface instance id. Mirrors the directive's instance..
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }
}
