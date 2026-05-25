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
/// Directive handler for Alexa control directives. Common flow:
/// <list type="number">
///   <item>Validate the directive payload.</item>
///   <item>Resolve the target HA entity (the payload transformer needs the current
///         entity to choose the right HA service call — e.g. light.turn_on vs.
///         light.turn_on with rgb).</item>
///   <item>Translate the directive into an HA service call and dispatch it.</item>
///   <item>Return an empty context immediately — Alexa picks up the resulting state
///         from the proactive <c>ChangeReport</c> emitted by
///         <see cref="Services.ChangeReportService"/> after HA's
///         <c>state_changed</c> event fires.</item>
/// </list>
/// </summary>
internal class ControlDirectiveHandler<TRequest>(IServiceProvider provider, string directiveName, IValidator<TRequest> validator, IOptions<JsonSerializerOptions> jsonOptions, ILogger<ControlDirectiveHandler<TRequest>> logger) : DirectiveHandler<TRequest, EmptyPayload>(validator, jsonOptions, logger)
    where TRequest : class
{
    private readonly IHomeAssistantClient client = provider.GetRequiredService<IHomeAssistantClient>();

    public override string DirectiveName => directiveName;

    protected sealed override async Task<(EmptyPayload, ContextProperty[]?)> HandleAsync(string? entityId, TRequest payload, CancellationToken cancellationToken)
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
            return (EmptyPayload.Instance, default);
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
