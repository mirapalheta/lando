using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives;

/// <summary>
/// Base class for SmartHome directive handlers.
/// Encapsulates common logic for executing Home Assistant service calls and returning responses.
/// Subclasses only need to define the request type via CreateRequest().
/// </summary>
internal abstract class DirectiveHandler<TRequest, TResponse>(IValidator<TRequest>? validator, IOptions<JsonSerializerOptions> jsonOptions, ILogger logger) : IDirectiveHandler
{
    /// <summary>
    /// Provides access to the logger for subclasses. The logger is scoped to the specific directive handler type for better log filtering.
    /// </summary>
    protected ILogger Logger => logger;

    /// <summary>
    /// Subclasses override to create the appropriate Home Assistant request.
    /// </summary>
    protected abstract Task<(TResponse, ContextProperty[]?)> HandleAsync(string? entityId, TRequest payload, CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract string DirectiveName { get; }

    protected virtual string Namespace => Namespaces.Alexa;
    protected virtual string EventName => EventNames.Response;

    /// <inheritdoc />
    public async Task<Response> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var messageId = request.Directive.Header.MessageId;
        var entityId = request.Directive.Endpoint?.EntityId();
        var payload = GetPayload(request);

        var loggedPayload = SecureString.WithRedactionEnabled(() => JsonSerializer.Serialize(payload, jsonOptions.Value));
        logger.LogInformation("[{DirectiveName}({MessageId})] Handling directive for EntityId: {EntityId} with payload: {Payload}", DirectiveName, messageId, entityId, loggedPayload);

        await (validator?.ValidateAndThrowAsync(payload, cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false);

        var (result, properties) = await HandleAsync(request.Directive.Endpoint?.EntityId(), payload, cancellationToken).ConfigureAwait(false);

        return request.Success(Namespace, EventName, result!, properties);
    }

    private TRequest GetPayload(Request request)
    {
        try
        {
            return JsonSerializer.Deserialize<TRequest>(
                request.Directive.Payload ?? throw new AlexaSmartHomeException(ErrorType.InvalidDirective, "Directive payload is required")
            ) ?? throw new AlexaSmartHomeException(ErrorType.InvalidDirective, "Invalid directive payload");
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "[{DirectiveName}({MessageId})] Failed to deserialize directive payload", DirectiveName, request.Directive.Header.MessageId);
            throw new AlexaSmartHomeException(ErrorType.InvalidDirective, "Invalid directive payload format");
        }
    }
}
