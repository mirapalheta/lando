using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives;

/// <summary>
/// Control-directive handler for the common, stateful case (TurnOn,
/// SetBrightness, Lock, …): returns an empty <c>Alexa.Response</c> and lets
/// <see cref="Services.ChangeReportService"/> report the resulting state change
/// proactively. This is the <see cref="EmptyPayload"/> specialization of the
/// generic base below.
/// </summary>
internal class ControlDirectiveHandler<TRequest>(IServiceProvider provider, string directiveName, IValidator<TRequest> validator, IOptions<JsonSerializerOptions> jsonOptions, ILogger<ControlDirectiveHandler<TRequest>> logger) : ControlDirectiveHandler<TRequest, EmptyPayload>(provider, directiveName, validator, jsonOptions, logger)
    where TRequest : class
{
    /// <inheritdoc />
    protected override EmptyPayload CreateResponse() => EmptyPayload.Instance;
}

/// <summary>
/// Generic dispatch base for entity-targeted directives. Common flow:
/// <list type="number">
///   <item>Validate the directive payload.</item>
///   <item>Resolve the target HA entity (the payload transformer needs the current
///         entity to choose the right HA service call — e.g. light.turn_on vs.
///         light.turn_on with rgb).</item>
///   <item>Translate the directive into an HA service call (via the keyed
///         <see cref="IPayloadTransform{TPayload}"/>) and dispatch it.</item>
///   <item>Return the subclass-supplied <see cref="CreateResponse"/> payload.</item>
/// </list>
/// </summary>
/// <remarks>
/// Subclasses choose the response contract via three hooks: <see cref="CreateResponse"/>
/// (the payload), and the inherited <c>Namespace</c>/<c>EventName</c> virtuals.
/// Stateful control directives (<see cref="ControlDirectiveHandler{TRequest}"/>)
/// return an <see cref="EmptyPayload"/> and rely on the proactive
/// <c>ChangeReport</c> from <see cref="Services.ChangeReportService"/>;
/// SceneController (<c>SceneDirectiveHandler</c>) instead returns a populated
/// payload synchronously in its own namespace.
/// </remarks>
internal abstract class ControlDirectiveHandler<TRequest, TResponse>(IServiceProvider provider, string directiveName, IValidator<TRequest> validator, IOptions<JsonSerializerOptions> jsonOptions, ILogger<ControlDirectiveHandler<TRequest, TResponse>> logger) : DirectiveHandler<TRequest, TResponse>(validator, jsonOptions, logger)
    where TRequest : class
{
    private readonly IHomeAssistantClient client = provider.GetRequiredService<IHomeAssistantClient>();

    /// <inheritdoc />
    public override string DirectiveName => directiveName;

    /// <summary>
    /// The response payload to return to Alexa. For control directives, this is always an empty payload; the actual state change is reported asynchronously via a <c>ChangeReport</c> event.
    /// </summary>
    protected abstract TResponse CreateResponse();

    protected sealed override async Task<(TResponse, ContextProperty[]?)> HandleAsync(string? entityId, TRequest payload, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await GetEntityAsync(
                entityId ?? throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"{DirectiveName} requires an endpoint"),
                cancellationToken
            ).ConfigureAwait(false);

            var request = provider
                .GetRequiredKeyedService<IPayloadTransform<TRequest>>(directiveName)
                .Transform(entity, payload);

            await client.CallServiceAsync(request, cancellationToken).ConfigureAwait(false);

            // No context properties: ChangeReportService will push the new state to Alexa
            // proactively. Returning empty context here is the documented pattern for
            // proactively-reported endpoints.
            return (CreateResponse(), default);
        }
        catch (Exception ex) when (ex is not AlexaSmartHomeException and not OperationCanceledException)
        {
            throw new AlexaSmartHomeException(ErrorType.EndpointUnreachable, $"Failed to execute {DirectiveName} for {entityId}: {ex.Message}", ex);
        }
    }

    private async Task<HomeAssistantEntity> GetEntityAsync(string entityId, CancellationToken cancellationToken)
        => await client.GetAsync(entityId, cancellationToken).ConfigureAwait(false)
        ?? throw new AlexaSmartHomeException(ErrorType.NoSuchEndpoint, $"Entity '{entityId}' not found");
}
