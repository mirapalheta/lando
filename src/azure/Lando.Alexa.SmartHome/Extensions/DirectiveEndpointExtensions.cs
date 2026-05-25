using System;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Models.Core;

using static Lando.Alexa.SmartHome.Constants;

/// <summary>
/// Conversions between Home Assistant's <c>domain.entity_name</c> entity-id format
/// and Alexa's <c>domain#entity_name</c> endpoint-id format. Alexa's endpoint id
/// disallows the dot character that HA uses, so the bridge substitutes <c>#</c> in
/// both directions.
/// </summary>
/// <remarks>
/// Keeping this seam in one place means the swap is centralized — a future change
/// to the separator (for example to support a future HA entity-id format) only
/// touches this file.
/// </remarks>
internal static class DirectiveEndpointExtensions
{
    /// <summary>
    /// Turns an Alexa endpoint id back into the HA entity id it was derived from.
    /// </summary>
    /// <remarks>
    /// Called on inbound directives, where the Alexa side identifies the target by
    /// the <c>#</c>-separated form. Throws when the endpoint or its id is missing,
    /// since neither shape is a recoverable input — the directive can't be routed.
    /// </remarks>
    /// <param name="endpoint">The directive endpoint Alexa sent.</param>
    /// <returns>
    /// The matching Home Assistant entity id, with <c>#</c> swapped back to <c>.</c>.
    /// </returns>
    public static string EntityId(this DirectiveEndpoint? endpoint)
        => endpoint?.EndpointId?.Replace(Separators.Alexa, Separators.HomeAssistant)
        ?? throw new InvalidOperationException("Directive endpoint is missing an EndpointId");

    /// <summary>
    /// Turns a Home Assistant entity id into the form Alexa wants on outbound
    /// discovery and state events.
    /// </summary>
    /// <remarks>
    /// Throws when the entity or its id is missing — an entity without an id cannot
    /// be discovered to Alexa and there's no safe default to substitute.
    /// </remarks>
    /// <param name="entity">The Home Assistant entity to convert.</param>
    /// <returns>
    /// The corresponding Alexa endpoint id, with <c>.</c> swapped to <c>#</c>.
    /// </returns>
    public static string EndpointId(this HomeAssistantEntity? entity)
        => entity?.EntityId?.Replace(Separators.HomeAssistant, Separators.Alexa)
        ?? throw new InvalidOperationException("Home Assistant entity is missing an EntityId");
}
