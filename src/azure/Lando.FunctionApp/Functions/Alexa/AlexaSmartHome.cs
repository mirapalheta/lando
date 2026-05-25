using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome;
using Lando.Alexa.SmartHome.Models.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Lando.FunctionApp.Functions.Alexa;

using static Lando.Alexa.SmartHome.Constants.Function;

/// <summary>
/// Azure Functions HTTP-trigger entry point for the Alexa Smart Home skill.
/// Thin wrapper over <see cref="FunctionBase{TRequest,TResponse}"/> — the
/// trigger declares its name, auth level, and route, and forwards everything
/// to the shared pipeline (body buffering, HMAC verification, deserialisation,
/// keyed-handler dispatch, response serialisation).
/// </summary>
public class AlexaSmartHome : FunctionBase<Request, Response>
{
    /// <summary>
    /// The Functions runtime entry point. Resolves the keyed validator and
    /// handler under <see cref="Constants.Function.Handler"/>
    /// and runs them via <see cref="FunctionBase{TRequest,TResponse}.HandleRequestAsync"/>.
    /// </summary>
    /// <param name="req">The inbound HTTP request from Alexa (via the AWS Lambda signer).</param>
    /// <param name="context">The Functions invocation context — used for DI scope and logging.</param>
    /// <param name="cancellationToken">Cancellation token tied to the invocation lifetime.</param>
    [Function(Name)]
    public Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Route)] HttpRequestData req,
        FunctionContext context, CancellationToken cancellationToken)
        => HandleRequestAsync(Handler, req, context, cancellationToken);
}
