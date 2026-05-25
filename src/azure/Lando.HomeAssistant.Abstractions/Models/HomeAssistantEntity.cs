using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lando.HomeAssistant.Models;

/// <summary>
/// JSON shape of a single Home Assistant entity as returned by the HA REST
/// API and the WebSocket <c>state_changed</c> event.
/// </summary>
/// <remarks>
/// Properties mirror the wire format byte-for-byte; helpers that interpret
/// the contents live on <c>HomeAssistantEntityExtensions</c> (domain
/// extraction, exposure flag, friendly-name resolution, attribute reads).
/// </remarks>
public class HomeAssistantEntity
{
    /// <summary>
    /// HA entity id in canonical form (e.g. <c>"light.living_room"</c>).
    /// </summary>
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = null!;

    /// <summary>
    /// HA entity state as a string. The interpretation is per-domain — e.g.
    /// <c>"on"</c> / <c>"off"</c> for switches, a numeric reading for sensors,
    /// <c>"unavailable"</c> when the integration cannot reach the device.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// HA <c>attributes</c> blob. Read through the typed extension methods on
    /// <c>HomeAssistantEntityAttributesExtensions</c> rather than indexing
    /// directly so domain-specific coercion stays in one place.
    /// </summary>
    [JsonPropertyName("attributes")]
    public Dictionary<string, object>? Attributes { get; set; }

    /// <summary>
    /// UTC timestamp of the last <c>state</c> transition.
    /// </summary>
    [JsonPropertyName("last_changed")]
    public DateTime? LastChanged { get; set; }

    /// <summary>
    /// UTC timestamp of the last refresh from HA — moves on every state poll,
    /// not just on transitions. Used as the <c>timeOfSample</c> stamp on
    /// Alexa context properties.
    /// </summary>
    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// HA event-context blob (parent_id, user_id, etc). Carried opaquely.
    /// </summary>
    [JsonPropertyName("context")]
    public object? Context { get; set; }
}
