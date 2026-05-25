using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.FunctionApp.Functions;

/// <summary>
/// Shared orchestration for HTTP-triggered Functions that authenticate via
/// HMAC and dispatch a typed request through an
/// <see cref="IRequestHandler{TRequest,TResponse}"/>.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline is, in order: buffer the body (with a size cap), authenticate
/// via the keyed <see cref="IRequestValidator"/>, deserialise into
/// <typeparamref name="TRequest"/> using the configured
/// <see cref="JsonSerializerOptions"/>, dispatch to the keyed
/// <see cref="IRequestHandler{TRequest,TResponse}"/>, and serialise the
/// response. Verifying before deserialising means malformed or unsigned
/// payloads never touch the JSON parser.
/// </para>
/// <para>
/// Subclasses are thin: declare a Function attribute and call
/// <see cref="HandleRequestAsync"/> with the DI key under which both the
/// validator and the handler were registered (see
/// <c>ServiceCollectionExtensions.AddRequestHandler&lt;,,&gt;</c>).
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The deserialised request type.</typeparam>
/// <typeparam name="TResponse">The response type produced by the handler.</typeparam>
public abstract class FunctionBase<TRequest, TResponse>
    where TRequest : class
{
    /// <summary>
    /// Hard upper bound on inbound request body size. Smart Home directives
    /// are kilobytes, so this is mostly a DoS guard — anything larger is
    /// rejected with <see cref="HttpStatusCode.RequestEntityTooLarge"/>
    /// before we allocate buffers or run any HMAC math.
    /// </summary>
    private const long MaxRequestBodySizeBytes = 6 * 1024 * 1024; // 6MB should be more than enough

    /// <summary>
    /// Buffer, validate, deserialise, dispatch, and write a response.
    /// </summary>
    /// <param name="key">
    /// The DI key shared by the <see cref="IRequestValidator"/> and the
    /// <see cref="IRequestHandler{TRequest,TResponse}"/> registered for this
    /// endpoint.
    /// </param>
    /// <param name="req">The Functions-worker HTTP request.</param>
    /// <param name="context">The Functions invocation context (used for DI scope and logging).</param>
    /// <param name="cancellationToken">Cancellation token tied to the invocation lifetime.</param>
    protected async Task<HttpResponseData> HandleRequestAsync(string key, HttpRequestData req, FunctionContext context, CancellationToken cancellationToken)
    {
        // Create activity for distributed tracing
        using var activity = new Activity(GetType().Name).Start();
        var correlationId = activity?.Id ?? Guid.NewGuid().ToString();
        var logger = context.GetLogger<FunctionBase<TRequest, TResponse>>();

        try
        {
            if (req.Body is null)
                throw new LandoException(HttpStatusCode.BadRequest, "Request body is required.");

            var validator = context.InstanceServices.GetRequiredKeyedService<IRequestValidator>(key);
            var handler = context.InstanceServices.GetRequiredKeyedService<IRequestHandler<TRequest, TResponse>>(key);
            var jsonOptions = context.InstanceServices.GetRequiredService<IOptions<JsonSerializerOptions>>();

            using var buffered = new MemoryStream();
            await req.Body.CopyToAsync(buffered, MaxRequestBodySizeBytes, cancellationToken).ConfigureAwait(false);
            validator.Validate(req.Headers, buffered.AsSpan(), correlationId);

            var request = await JsonSerializer.DeserializeAsync<TRequest>(buffered.Reset(), jsonOptions.Value, cancellationToken).ConfigureAwait(false)
                ?? throw new LandoException(HttpStatusCode.BadRequest, "Invalid request: unable to deserialize payload");

            var response = await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);

            return await CreateResponseAsync(req, HttpStatusCode.OK, response, cancellationToken).ConfigureAwait(false);
        }
        catch (LandoException lex)
        {
            logger.LogError(lex, "Known error processing request [CorrelationId: {CorrelationId}, Message: {Message}]", correlationId, lex.Message);
            return await CreateResponseAsync(req, lex.StatusCode, default, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing request [CorrelationId: {CorrelationId}]", correlationId);
            return await CreateResponseAsync(req, HttpStatusCode.InternalServerError, default, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Builds an <see cref="HttpResponseData"/> with <paramref name="statusCode"/>
    /// and (optionally) a JSON-serialised <paramref name="body"/>. Uses the
    /// container's configured <see cref="JsonSerializerOptions"/> via
    /// <see cref="HttpResponseDataExtensions.WriteAsJsonAsync{T}(HttpResponseData, T, System.Threading.CancellationToken)"/>.
    /// </summary>
    private static async ValueTask<HttpResponseData> CreateResponseAsync(HttpRequestData req, HttpStatusCode statusCode, object? body, CancellationToken cancellationToken)
    {
        var response = req.CreateResponse(statusCode);
        if (body != null)
        {
            await response.WriteAsJsonAsync(body, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        return response;
    }
}
