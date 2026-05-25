using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;

namespace Lando.Alexa.SmartHome;

/// <summary>
/// Strategy interface for SmartHome directive handlers.
/// Each directive (TurnOn, SetBrightness, etc.) has its own handler implementation.
/// </summary>
public interface IDirectiveHandler
{
    /// <summary>
    /// Unique name matching the SmartHome Header.Name field (e.g., "TurnOn", "Discover")
    /// </summary>
    string DirectiveName { get; }

    /// <summary>
    /// Handles a single directive and returns the response
    /// </summary>
    Task<Response> HandleAsync(Request request, CancellationToken cancellationToken);
}
