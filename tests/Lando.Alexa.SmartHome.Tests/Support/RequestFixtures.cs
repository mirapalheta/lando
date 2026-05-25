using System;
using System.Text.Json;
using Lando.Alexa.SmartHome.Models.Core;

namespace Lando.Alexa.SmartHome.Tests.Support;

/// <summary>
/// Centralises construction of inbound <see cref="Request"/> envelopes so each
/// handler/orchestrator test states only the fields it cares about. Mirrors the
/// shape of envelopes the Azure Function deserialises from the wire — round-tripping
/// through <see cref="JsonSerializer"/> for the payload so the JsonElement the
/// production code consumes is byte-identical to what Alexa would send.
/// </summary>
/// <remarks>
/// The default skill-targeted directive (no endpoint, empty payload) is what
/// the orchestrator dispatcher already accepts; callers override the bits they
/// care about — endpoint, payload, correlation token — for per-handler tests.
/// </remarks>
internal static class RequestFixtures
{
    /// <summary>
    /// Builds a directive envelope with the given header/endpoint/payload. Defaults
    /// produce a skill-targeted directive (no endpoint) with an empty payload, which
    /// the <c>SmartHomeHandler</c> dispatcher already accepts.
    /// </summary>
    public static Request Directive(
        string @namespace,
        string name,
        object? payload = null,
        DirectiveEndpoint? endpoint = null,
        string? correlationToken = "corr-token",
        string? instance = null,
        string? messageId = null) => new()
        {
            Directive = new Directive
            {
                Header = new DirectiveHeader
                {
                    Namespace = @namespace,
                    Name = name,
                    MessageId = messageId ?? "msg-" + Guid.NewGuid().ToString("N"),
                    CorrelationToken = new SecureString(correlationToken),
                    PayloadVersion = PayloadVersion.V3,
                    Instance = instance,
                },
                Endpoint = endpoint,
                Payload = AsElement(payload ?? new { }),
            },
        };

    /// <summary>
    /// Builds a device-targeted directive endpoint (Power/Brightness/Lock/etc.).
    /// </summary>
    public static DirectiveEndpoint Endpoint(
        string endpointId = "light#living_room",
        string scopeToken = "alexa-bearer-token")
        => new()
        {
            EndpointId = endpointId,
            Scope = new Scope { Type = "BearerToken", Token = new SecureString(scopeToken) },
        };

    /// <summary>
    /// Serialise the payload through System.Text.Json so the JsonElement consumers
    /// observe is identical to what a real inbound directive would deliver.
    /// </summary>
    public static JsonElement AsElement(object payload)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        return doc.RootElement.Clone();
    }
}
