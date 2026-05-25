using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Top-level wire object the bridge returns to Alexa: <c>{ "event": {...}, "context"?: {...} }</c>.
/// </summary>
/// <remarks>
/// <para>
/// Use the constructor or the <c>Success</c> / <c>Error</c> extension methods in
/// <c>RequestExtensions</c> to keep correlation tokens and message ids wired up correctly.
/// </para>
/// </remarks>
public sealed class Response
{
    /// <summary>
    /// The outbound event (response, change report, error, etc.)..
    /// </summary>
    [JsonPropertyName("event")]
    public Event Event { get; set; } = new();

    /// <summary>
    /// Optional context with current property values. Required for device-targeted
    /// responses that change state; omit on Discovery/AcceptGrant responses.
    /// </summary>
    [JsonPropertyName("context")]
    public Context? Context { get; set; }
}
