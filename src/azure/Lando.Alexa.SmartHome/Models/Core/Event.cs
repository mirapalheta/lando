using System;
using System.Text.Json.Serialization;

namespace Lando.Alexa.SmartHome.Models.Core;

/// <summary>
/// Outbound event body — the <c>event</c> object inside an <see cref="Response"/>.
/// Carries the header, optional endpoint, and a polymorphic <see cref="Payload"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Payload"/> is typed as <see cref="object"/> on purpose: it can be any of the
/// per-interface payload models, an <see cref="EmptyPayload"/>, or an
/// <see cref="ErrorResponse.ErrorPayload"/>. System.Text.Json serializes the concrete
/// runtime type when the container's default-polymorphism settings are in effect.
/// </para>
/// <para>
/// We deliberately avoid a generic <c>Event&lt;T&gt;</c> sub-type because hiding the
/// <c>payload</c> property with <c>new</c> trips System.Text.Json into emitting it twice.
/// </para>
/// </remarks>
public sealed class Event
{
    [JsonPropertyName("header")]
    public EventHeader Header { get; set; } = new();

    [JsonPropertyName("endpoint")]
    public EventEndpoint? Endpoint { get; set; }

    [JsonPropertyName("payload")]
    public object Payload { get; set; } = EmptyPayload.Instance;

    public static Event Create(string @namespace, string name, object payload, EventEndpoint? endpoint = null, string? correlationToken = null, string? instance = null)
        => new()
        {
            Header = new()
            {
                Namespace = @namespace,
                Name = name,
                MessageId = Guid.NewGuid().ToString(),
                CorrelationToken = new(correlationToken),
                PayloadVersion = PayloadVersion.V3,
                Instance = instance
            },
            Endpoint = endpoint,
            Payload = payload
        };
}
