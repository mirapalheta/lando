using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives;

using static Lando.Alexa.SmartHome.Constants;

/// <summary>
/// Handles the <c>Alexa.ReportState</c> directive — Alexa asks "what's the
/// current state of this endpoint?" and the bridge responds with an
/// <c>Alexa.StateReport</c> event whose <c>context.properties</c> block
/// carries the current value of every retrievable property the endpoint
/// advertised at discovery.
/// </summary>
/// <remarks>
/// The per-domain HA → Alexa value translation lives in the registered
/// <see cref="IEntityTransform{T}"/> implementations; this handler just
/// orchestrates the fetch and dispatch. A missing transformer is treated as
/// an internal invariant violation — every supported HA domain must
/// register both a discovery and a state transformer — so the surfaced
/// error type is <see cref="ErrorType.InternalError"/> rather than
/// <see cref="ErrorType.InvalidDirective"/>.
/// </remarks>
internal class ReportStateDirectiveHandler(IHomeAssistantClient client, IEntityTransform<ContextProperty[]> transformer, IValidator<EmptyPayload> validator, IOptions<JsonSerializerOptions> jsonOptions, ILogger<ReportStateDirectiveHandler> logger)
    : DirectiveHandler<EmptyPayload, EmptyPayload>(validator, jsonOptions, logger)
{
    /// <inheritdoc />
    public override string DirectiveName => DirectiveNames.ReportState;

    /// <inheritdoc />
    protected override string Namespace => Namespaces.Alexa;

    /// <inheritdoc />
    protected override string EventName => EventNames.StateReport;

    /// <inheritdoc />
    protected override async Task<(EmptyPayload, ContextProperty[]?)> HandleAsync(string? entityId, EmptyPayload payload, CancellationToken cancellationToken)
    {
        var entity = await client.GetAsync(
            entityId ?? throw new AlexaSmartHomeException(ErrorType.InvalidDirective, "ReportState requires an endpoint"),
            cancellationToken
        ).ConfigureAwait(false);

        if (entity?.IsExposed(CustomAttributes.Expose) != true)
            throw new AlexaSmartHomeException(ErrorType.NoSuchEndpoint, $"Entity '{entityId}' not found");

        var properties = transformer.Transform(entity)
            ?? throw new AlexaSmartHomeException(ErrorType.NoSuchEndpoint, $"Entity '{entityId}' not found");

        return (EmptyPayload.Instance, [.. properties]);
    }
}
