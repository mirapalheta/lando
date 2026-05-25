using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;

namespace Lando.Alexa.SmartHome;

/// <summary>
/// Posts proactive events to the Alexa Event Gateway
/// (<c>https://api.amazonalexa.com/v3/events</c>).
/// </summary>
public interface IEventGatewayClient
{
    /// <summary>
    /// Sends a <c>ChangeReport</c> event for a single endpoint.
    /// </summary>
    /// <param name="endpointId">The Alexa endpoint id (HA entity id).</param>
    /// <param name="changed">Properties whose values changed (reported in <c>change.properties</c>).</param>
    /// <param name="all">All retrievable properties for the endpoint (reported in <c>context.properties</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<bool> SendChangeReportAsync(string endpointId, ContextProperty[] changed, ContextProperty[] all, CancellationToken cancellationToken);
}
