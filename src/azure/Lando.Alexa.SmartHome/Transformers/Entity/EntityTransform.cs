using System;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// Composite dispatcher that implements every closed form of
/// <see cref="IEntityTransform{T}"/> the bridge ships with — currently
/// <c>DiscoveryEndpoint</c> and <c>IReadOnlyList&lt;ContextProperty&gt;</c>.
/// Looks up the per-domain transformer registered under
/// <c>entity.GetDomain()</c> in DI and delegates the projection to it.
/// </summary>
/// <remarks>
/// This is intentionally a chain-of-responsibility shape: the dispatcher
/// itself satisfies <see cref="IEntityTransform{T}"/>, but its
/// implementation never produces a result on its own — it always defers to a
/// keyed instance registered for the entity's domain.
/// <para>
/// The class is registered in DI without a key for each closed form it
/// implements; the per-domain transformers are registered with the domain
/// name as the key. The two registrations don't collide because keyed
/// lookups never resolve unkeyed services, and the dispatcher's own lookup
/// is always keyed.
/// </para>
/// <para>
/// Returning <c>null</c> from <see cref="Transform{T}"/> is reserved for the
/// case where no per-domain transformer is registered. Consumers should
/// treat that as an internal invariant violation — every supported HA
/// domain must register a transformer for every output shape the bridge
/// uses, and the discovery / state pair must move in lockstep.
/// </para>
/// </remarks>
public class EntityTransform(IServiceProvider provider) : IEntityTransform<DiscoveryEndpoint>, IEntityTransform<ContextProperty[]>
{
    /// <inheritdoc />
    DiscoveryEndpoint? IEntityTransform<DiscoveryEndpoint>.Transform(HomeAssistantEntity entity)
        => Transform<DiscoveryEndpoint>(entity);

    /// <inheritdoc />
    ContextProperty[]? IEntityTransform<ContextProperty[]>.Transform(HomeAssistantEntity entity)
        => Transform<ContextProperty[]>(entity);

    /// <summary>
    /// Resolves the per-domain <see cref="IEntityTransform{T}"/> keyed under
    /// the entity's domain and delegates to it. Returns <c>null</c> when no
    /// transformer is registered for the domain — the closed-form interface
    /// methods propagate that null to the caller.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="ServiceProviderKeyedServiceExtensions.GetKeyedService{T}(IServiceProvider, object?)"/>
    /// (not <c>GetRequiredKeyedService</c>) so unregistered domains return
    /// null rather than throw — the call site decides how to surface the
    /// missing-transformer condition (typically as an internal error).
    /// </remarks>
    /// <param name="entity">The entity to project.</param>
    /// <typeparam name="T">The Alexa-facing shape to project into.</typeparam>
    /// <returns>
    /// The projected value, or <c>null</c> when no transformer is registered
    /// for the entity's domain.
    /// </returns>
    private T? Transform<T>(HomeAssistantEntity entity) where T : class
        => provider.GetKeyedService<IEntityTransform<T>>(entity.GetDomain())?.Transform(entity);
}
