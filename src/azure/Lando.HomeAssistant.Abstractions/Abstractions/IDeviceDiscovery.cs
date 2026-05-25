using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Models;

namespace Lando.HomeAssistant;

/// <summary>
/// Surfaces the controllable Home Assistant entities that downstream integrations
/// (Alexa, Google Home, etc.) operate on. Implementations are responsible for filtering
/// entities by domain and exposure so callers can treat the result as the
/// authoritative set of entities the bridge will act upon.
/// </summary>
/// <remarks>
/// The previous shape mapped HA entities into an intermediate <c>HomeAssistantDevice</c>
/// projection before handing them to consumers. That projection has been removed —
/// callers consume <see cref="HomeAssistantEntity"/> directly and read attributes
/// through the typed extension methods on
/// <see cref="HomeAssistantEntityExtensions"/>. Integration-specific exposure layering
/// (for example an <c>alexa_expose</c> flag on top of the bridge-wide
/// <c>lando_expose</c>) is owned by the consumer, not this interface.
/// </remarks>
public interface IDeviceDiscovery
{
    /// <summary>
    /// Streams every controllable, lando-exposed entity currently visible to the bridge.
    /// </summary>
    /// <remarks>
    /// Filters out entities whose domain is not in the bridge's controllable-domain
    /// set (see <see cref="Constants.Domains"/>) and entities without a truthy
    /// <c>lando_expose</c> attribute. The async-enumerable shape lets callers project
    /// without materializing the whole HA state catalogue when only a few entities are
    /// relevant.
    /// </remarks>
    /// <param name="cancellationToken">
    /// Cooperative cancellation token; cancelling propagates into the underlying HTTP
    /// request so an in-flight enumeration completes promptly.
    /// </param>
    /// <returns>
    /// An asynchronous stream of <see cref="HomeAssistantEntity"/> values. Order is
    /// HA's natural enumeration order and may change between calls; consumers must
    /// not rely on it.
    /// </returns>
    IAsyncEnumerable<HomeAssistantEntity> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the live state of a single controllable, lando-exposed entity.
    /// </summary>
    /// <remarks>
    /// Used by per-directive state lookups (for example the Alexa <c>ReportState</c>
    /// path and the <c>Adjust*</c> directives that need the current value before
    /// computing the new one). Returns <c>null</c> when the entity does not exist, is
    /// not in a controllable domain, or is not exposed via <c>lando_expose</c>.
    /// </remarks>
    /// <param name="entityId">
    /// HA entity id in canonical form, for example <c>"light.living_room"</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// Cooperative cancellation token; honoured for the duration of the HTTP request.
    /// </param>
    /// <returns>
    /// The live <see cref="HomeAssistantEntity"/>, or <c>null</c> when the entity is
    /// not present, not controllable, or not exposed.
    /// </returns>
    Task<HomeAssistantEntity?> GetAsync(string entityId, CancellationToken cancellationToken = default);
}
