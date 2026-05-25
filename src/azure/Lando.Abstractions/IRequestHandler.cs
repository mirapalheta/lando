using System.Threading;
using System.Threading.Tasks;

namespace Lando;

/// <summary>
/// Dispatch seam for a single deserialised request to its handler. One
/// implementation per request type — Smart Home directives, Custom Skill
/// intents, future MCP requests, and so on. Keeping this interface narrow
/// (no headers, no streams, no HTTP plumbing) is what lets the same handler
/// be reused from any transport: function trigger, in-process test, or
/// future MCP gateway.
/// </summary>
/// <remarks>
/// Implementations should be unit-testable with test doubles for their own
/// downstream collaborators (e.g. an <c>IHomeAssistantClient</c> fake) and
/// must not assume an HTTP context — that's owned by
/// <c>FunctionBase&lt;TRequest,TResponse&gt;</c>.
/// </remarks>
/// <typeparam name="TRequest">The deserialised request type this handler accepts.</typeparam>
/// <typeparam name="TResponse">The response type this handler produces.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : class
{
    /// <summary>
    /// Processes a request and returns its response.
    /// </summary>
    /// <param name="request">The deserialised request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response object to serialise back to the caller.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
