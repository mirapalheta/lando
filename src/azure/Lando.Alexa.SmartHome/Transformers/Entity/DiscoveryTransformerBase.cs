using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.Alexa.SmartHome.Constants;

/// <summary>
/// Shared base for per-domain discovery transformers. Centralises the parts
/// of a <see cref="DiscoveryEndpoint"/> that don't vary by domain —
/// endpoint id, manufacturer, sanitised friendly name, description — so
/// subclasses only need to choose the display category and capabilities.
/// </summary>
/// <remarks>
/// Implements the <c>DiscoveryEndpoint</c> closed form of
/// <see cref="IEntityTransform{T}"/>. The friendly-name sanitiser strips
/// characters Alexa disallows in endpoint names; HA users routinely put
/// apostrophes and accented characters in <c>friendly_name</c> and Alexa
/// rejects the whole endpoint if any of them sneak through.
/// </remarks>
public abstract class DiscoveryTransformerBase : IEntityTransform<DiscoveryEndpoint>
{
    /// <inheritdoc />
    public DiscoveryEndpoint Transform(HomeAssistantEntity entity)
        => DiscoveryEndpoint.Create(
            endpointId: entity.EndpointId(),
            friendlyName: entity.GetFriendlyName(CustomAttributes.Name),
            category: entity.Attributes.GetString(CustomAttributes.Display) ?? GetDisplayCategory(entity),
            capabilities: BuildCapabilities(entity)
        );

    /// <summary>
    /// Returns the single Alexa display category that best describes the
    /// entity.
    /// </summary>
    /// <remarks>
    /// Subclasses typically branch on <c>device_class</c> — covers in
    /// particular split into blinds, doors, garages, awnings, and curtains,
    /// each with its own Alexa category.
    /// </remarks>
    /// <param name="entity">The entity being discovered.</param>
    /// <returns>One of the <see cref="DisplayCategory"/> constants.</returns>
    protected abstract string GetDisplayCategory(HomeAssistantEntity entity);

    /// <summary>
    /// Returns the domain-specific Alexa capabilities for the entity.
    /// </summary>
    /// <remarks>
    /// The default <c>Alexa</c> and <c>Alexa.EndpointHealth</c> capabilities
    /// are already prepended in <see cref="BuildCapabilities"/> —
    /// subclasses only return the per-domain capabilities.
    /// </remarks>
    /// <param name="entity">The entity being discovered.</param>
    /// <returns>The per-domain capabilities to advertise.</returns>
    protected abstract IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity);

    /// <summary>
    /// Composes the full capabilities list — defaults first, then
    /// per-domain — that will be embedded in the discovery endpoint.
    /// </summary>
    /// <remarks>
    /// Lives on the base class so every transformer guarantees the
    /// <see cref="Capability.DefaultCapabilities"/> are present and in a
    /// consistent order; missing them would make Alexa flag the endpoint as
    /// misbehaving.
    /// </remarks>
    /// <param name="entity">The entity being discovered.</param>
    /// <returns>Defaults concatenated with the per-domain capabilities.</returns>
    private List<Capability> BuildCapabilities(HomeAssistantEntity entity)
        =>
        [
            .. Capability.DefaultCapabilities,
            .. GetDomainCapabilities(entity)
        ];
}
