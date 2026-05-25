using System;
using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.EndpointHealth;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

/// <summary>
/// Shared base for per-domain state transformers. Stamps the
/// <c>Alexa.EndpointHealth</c> property on every state report so each
/// subclass only has to produce the domain-specific properties, and exposes
/// <see cref="DefaultUncertaintyMs"/> so per-domain context properties
/// agree on freshness metadata.
/// </summary>
/// <remarks>
/// Implements the <c>IReadOnlyList&lt;ContextProperty&gt;</c> closed form of
/// <see cref="IEntityTransform{T}"/>. Per-domain implementations override
/// <see cref="GetDomainProperties"/>; the base class composes that with the
/// EndpointHealth property the Alexa runtime expects on every endpoint.
/// </remarks>
public abstract class StateTransformerBase : IEntityTransform<ContextProperty[]>
{
    /// <summary>
    /// Default <c>uncertaintyInMilliseconds</c> stamped on each reported
    /// property.
    /// </summary>
    /// <remarks>
    /// HA's REST API returns a point-in-time snapshot of state, but the
    /// underlying entity may have been polled a few seconds prior. 1000ms is
    /// an honest upper bound for most integrations and gives Alexa a
    /// reasonable freshness signal.
    /// </remarks>
    protected const int DefaultUncertaintyMs = 1000;

    /// <inheritdoc />
    public ContextProperty[] Transform(HomeAssistantEntity entity)
        =>
        [
            BuildEndpointHealth(entity),
            .. GetDomainProperties(entity)
        ];

    /// <summary>
    /// Returns the domain-specific properties that make up the bulk of the
    /// state report — for example PowerState + Brightness for lights,
    /// ThermostatMode + TargetSetpoint for climate.
    /// </summary>
    /// <remarks>
    /// Implementations must report a value for every retrievable property
    /// the matching discovery transformer advertised; advertising a
    /// retrievable capability without backing it with a state value makes
    /// Alexa mark the endpoint unhealthy.
    /// </remarks>
    /// <param name="entity">The entity whose state to project.</param>
    /// <returns>The per-domain context properties to report.</returns>
    protected abstract IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity);

    /// <summary>
    /// Stamps the <c>Alexa.EndpointHealth</c> context property that Alexa
    /// expects on every endpoint, mapping HA's <c>unavailable</c> state to
    /// <see cref="ConnectivityValue.Unreachable"/> and everything else to
    /// <see cref="ConnectivityValue.Ok"/>.
    /// </summary>
    /// <remarks>
    /// Reporting <c>UNREACHABLE</c> when HA can't talk to the underlying
    /// integration lets the Alexa app show "device is unresponsive" instead
    /// of a stale value the user might mistakenly act on.
    /// </remarks>
    /// <param name="entity">The entity whose health to report.</param>
    /// <returns>The endpoint-health context property.</returns>
    private static ContextProperty BuildEndpointHealth(HomeAssistantEntity entity) => new()
    {
        Namespace = Namespaces.EndpointHealth,
        Name = EndpointHealthProperties.Connectivity,
        Value = new Connectivity
        {
            Value = string.Equals(entity.State, "unavailable", StringComparison.OrdinalIgnoreCase)
                ? ConnectivityValue.Unreachable
                : ConnectivityValue.Ok
        },
        TimeOfSample = entity.LastUpdated,
        UncertaintyInMilliseconds = DefaultUncertaintyMs
    };
}
