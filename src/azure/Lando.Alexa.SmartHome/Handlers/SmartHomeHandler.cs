using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers;

/// <summary>
/// Top-level dispatcher for Alexa Smart Home skill requests. Validates the
/// inbound envelope and routes the directive to the
/// <see cref="IDirectiveHandler"/> registered under the directive's name.
/// </summary>
/// <remarks>
/// This handler keeps an <see cref="IServiceProvider"/> only because the
/// per-directive handlers are registered as keyed services and the
/// directive name is the key — the lookup is fundamentally a runtime
/// resolution. The fixed-type dependencies (<see cref="IValidator{T}"/>,
/// the logger) are injected directly so they don't go through service
/// location.
/// </remarks>
public class SmartHomeHandler(IServiceProvider provider, IValidator<Request> validator, IOptions<JsonSerializerOptions> jsonOptions, ILogger<SmartHomeHandler> logger) : IRequestHandler<Request, Response>
{
    /// <inheritdoc />
    public async Task<Response> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        try
        {
            var directive = request.Directive.Header.Name;
            var messageId = request.Directive.Header.MessageId;
            var entityId = request.Directive.Endpoint?.EntityId();

            var loggedRequest = SecureString.WithRedactionEnabled(() => JsonSerializer.Serialize(request, jsonOptions.Value));
            logger.LogInformation("[{DirectiveName}({MessageId})] Request({EntityId}): {Request}", directive, messageId, entityId, loggedRequest);

            if (provider.GetKeyedService<IDirectiveHandler>(directive) is not IDirectiveHandler handler)
                throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"No handler found for directive: {directive}");

            await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

            // logger.LogInformation("Received directive: {DirectiveName}, MessageId: {MessageId}, EntityId: {EntityId}", directive, messageId, entityId);

            logger.LogInformation("Smart Home directive received: {DirectiveName}", directive);

            var response = await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);

            var loggedResponse = SecureString.WithRedactionEnabled(() => JsonSerializer.Serialize(response, jsonOptions.Value));
            logger.LogInformation("[{DirectiveName}({MessageId})] Response({EntityId}): {Response}", directive, messageId, entityId, loggedResponse);

            return response;
        }
        catch (ValidationException ex)
        {
            logger.LogError(ex, "Request validation failed");
            return request.Error(ErrorType.InvalidDirective, $"Directive validation failed");
        }
        catch (AlexaSmartHomeException ex)
        {
            logger.LogWarning(ex, "{Message}: {Error}", ex.Message, ex.Error);
            return request.Error(ex.Error, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Smart Home directive");
            return request.Error(ErrorType.InternalError, "An internal error occurred while processing the directive");
        }
    }
}
