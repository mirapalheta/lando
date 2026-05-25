using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome;

/// <summary>
/// Projects a <see cref="HomeAssistantEntity"/> into some Alexa-facing shape
/// <typeparamref name="T"/>. One generic interface covers both per-domain
/// transformers (where the implementation is the actual projection) and the
/// composite dispatcher (where the implementation routes to a keyed
/// per-domain instance).
/// </summary>
/// <remarks>
/// Two closed forms ship with the bridge:
/// <list type="bullet">
///   <item><description>
///     <c>IEntityTransform&lt;DiscoveryEndpoint&gt;</c> — used at discovery
///     time to project entities into the Alexa endpoint shape.
///   </description></item>
///   <item><description>
///     <c>IEntityTransform&lt;IReadOnlyList&lt;ContextProperty&gt;&gt;</c> —
///     used at state-report time to project entity state into the property
///     snapshot Alexa expects in the context block.
///   </description></item>
/// </list>
/// The dispatcher resolves the per-domain transformer through DI keyed by
/// <c>entity.GetDomain()</c>, so adding a new HA domain is one new file plus
/// one keyed registration for each output shape the bridge supports.
/// </remarks>
/// <typeparam name="T">
/// The Alexa-facing shape produced by the transformation. Constrained to
/// reference types so the nullable annotation on <see cref="Transform"/>'s
/// return type unambiguously means "null when no transformer matched."
/// </typeparam>
public interface IEntityTransform<T> where T : class
{
    /// <summary>
    /// Projects the given entity into the target shape, or returns
    /// <c>null</c> when no projection is available — typically because the
    /// dispatcher could not resolve a registered per-domain transformer for
    /// <c>entity.GetDomain()</c>.
    /// </summary>
    /// <remarks>
    /// Per-domain implementations are expected to always return a non-null
    /// value: they're only reached when DI has already matched the entity's
    /// domain. The dispatcher is the one site that returns <c>null</c>, and
    /// callers should treat null as an internal invariant violation (every
    /// supported domain ought to have both Discovery and State transformers
    /// registered).
    /// </remarks>
    /// <param name="entity">The entity to project.</param>
    /// <returns>
    /// The Alexa-facing shape.
    /// </returns>
    T? Transform(HomeAssistantEntity entity);
}
