using System.Collections.Generic;
using System.Threading;
using Lando.HomeAssistant.Models;

namespace Lando.HomeAssistant;

/// <summary>
/// Subscribes to Home Assistant entity state changes via the HA WebSocket API.
/// </summary>
/// <remarks>
/// The implementation connects to <c>wss://ha-host/api/websocket</c>, authenticates
/// with the long-lived token from configuration, and streams <c>state_changed</c> events
/// until the caller's cancellation token is cancelled or the connection drops.
/// The caller is responsible for reconnect logic (e.g. via a retry loop in an
/// <c>IHostedService</c>).
/// </remarks>
public interface IHomeAssistantWebSocketClient
{
    /// <summary>
    /// Opens a WebSocket connection to the HA event bus and yields every
    /// <c>state_changed</c> event until the token is cancelled or the connection is lost.
    /// </summary>
    /// <param name="cancellationToken">Token used to stop the subscription.</param>
    IAsyncEnumerable<HomeAssistantStateChangedEvent> SubscribeAsync(CancellationToken cancellationToken = default);
}
