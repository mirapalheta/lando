using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Models;

namespace Lando.HomeAssistant.Services;

/// <summary>
/// Composite Home Assistant client that delegates discovery to an
/// <see cref="IDeviceDiscovery"/> and service calls to an <see cref="IServiceCaller"/>.
/// Exposed as the single application-level seam over HA so consumers can depend on
/// one interface instead of two.
/// </summary>
/// <remarks>
/// Composition rather than inheritance lets each underlying responsibility be tested
/// and swapped independently — for instance the service-caller can be replaced with
/// a recording fake during integration tests without standing up a fake HTTP layer
/// for discovery.
/// </remarks>
public class HomeAssistantClient(IDeviceDiscovery deviceDiscovery, IServiceCaller serviceCaller) : IHomeAssistantClient
{
    /// <inheritdoc />
    public IAsyncEnumerable<HomeAssistantEntity> ListAsync(CancellationToken cancellationToken = default)
        => deviceDiscovery.ListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<HomeAssistantEntity?> GetAsync(string entityId, CancellationToken cancellationToken = default)
        => deviceDiscovery.GetAsync(entityId, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// Issues the call <see cref="HomeAssistantRequest.Repeat"/> times, sequentially,
    /// so step-style services (volume_up/down) land in order and a failure stops
    /// the remaining repeats.
    /// </remarks>
    public async Task CallServiceAsync(HomeAssistantRequest data, CancellationToken cancellationToken)
    {
        for (var i = 0; i < data.Repeat; i++)
            await serviceCaller.CallServiceAsync(data, cancellationToken).ConfigureAwait(false);
    }
}
