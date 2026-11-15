using System.Collections.Generic;
using System.Text.Json;
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

    /// <summary>
    /// Runs a script as its own service (<c>script.&lt;object_id&gt;</c>) with
    /// <c>return_response</c>, waits for it to finish, and returns what the
    /// script's <c>stop:</c> step handed back in <c>response_variable</c>.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="HomeAssistantRequest.RunScript"/> (<c>script.turn_on</c>,
    /// fire-and-forget), this blocks until the script ends, so only use it for
    /// scripts that are known to finish quickly, and bound it with
    /// <paramref name="cancellationToken"/>. HA shields the call: a client that
    /// gives up does not cancel the script.
    /// </remarks>
    /// <param name="entityId">Script entity id, e.g. <c>script.example_routine</c>.</param>
    /// <param name="variables">The script's fields, sent as the flat request body. Null or empty sends <c>{}</c>.</param>
    /// <param name="cancellationToken">Cancellation token honoured for the duration of the HTTP request.</param>
    /// <returns>
    /// A detached copy of the <c>service_response</c> object, or null when HA's
    /// answer has none (a script that returns nothing gives <c>{}</c>).
    /// </returns>
    Task<JsonElement?> RunScriptWithResponseAsync(string entityId, IReadOnlyDictionary<string, object?>? variables, CancellationToken cancellationToken);
}
