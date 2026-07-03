using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.CustomSkill.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Lando.FunctionApp.Functions.Alexa;

using static Lando.Alexa.CustomSkill.Constants.Function;

/// <summary>
/// Azure Functions HTTP-trigger entry point for the Alexa Custom Skill (intent)
/// path. Thin wrapper over <see cref="FunctionBase{TRequest,TResponse}"/>: the
/// trigger declares its name, auth level, and route, and forwards to the shared
/// pipeline (body buffering, HMAC verification, deserialisation, keyed-handler
/// dispatch, response serialisation). The AWS Lambda routes custom-skill
/// payloads here while Smart Home directives continue to hit
/// <see cref="AlexaSmartHome"/>.
/// </summary>
public class AlexaIntent : FunctionBase<IntentRequest, IntentResponse>
{
    /// <summary>
    /// The Functions runtime entry point for custom-skill intents.
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
