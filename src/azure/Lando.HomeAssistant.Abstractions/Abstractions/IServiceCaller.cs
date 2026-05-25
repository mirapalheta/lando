using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Models;

namespace Lando.HomeAssistant;

/// <summary>
/// Calls Home Assistant services (the HA-side equivalent of a write — for example
/// <c>light.turn_on</c> or <c>climate.set_temperature</c>). Single responsibility:
/// service calls only — discovery and state reads belong to
/// <see cref="IDeviceDiscovery"/>.
/// </summary>
public interface IServiceCaller
{
    /// <summary>
    /// Calls a Home Assistant service. The service domain and payload are baked
    /// into the supplied <paramref name="data"/>; implementations resolve the
    /// HA service endpoint from <see cref="HomeAssistantRequest.Service"/> and
    /// <see cref="HomeAssistantRequest.EntityId"/>.
    /// </summary>
    /// <param name="data">The service call data, including entity id and service name.</param>
    /// <param name="cancellationToken">Cancellation token honoured for the duration of the HTTP request.</param>
    Task CallServiceAsync(HomeAssistantRequest data, CancellationToken cancellationToken);
}
