using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives;

using static Lando.Alexa.SmartHome.Constants;

/// <summary>
/// Handles the <c>Alexa.Discovery.Discover</c> directive. Streams every controllable,
/// lando-exposed entity out of Home Assistant and routes each one through the
/// <see cref="IEntityTransform{T}"/> dispatcher to produce its matching
/// <see cref="DiscoveryEndpoint"/>. Entities whose domain isn't claimed by a
/// registered transformer are silently dropped so a partial deploy still yields a
/// valid discovery response.
/// </summary>
/// <remarks>
/// All the per-domain branching used to live inline here behind a giant LINQ
/// projection. Pushing it into per-domain <see cref="IEntityTransform{T}"/> implementations
/// means this handler stays small and stable as new domains come online — adding a
/// new transformer alone is enough to surface a new domain.
/// </remarks>
internal class DiscoverDirectiveHandler(IHomeAssistantClient client, IEntityTransform<DiscoveryEndpoint> transformer, IValidator<DiscoveryDirectivePayload> validator, IOptions<JsonSerializerOptions> jsonOptions, ILogger<DiscoverDirectiveHandler> logger)
    : DirectiveHandler<DiscoveryDirectivePayload, DiscoveryResponsePayload>(validator, jsonOptions, logger)
{
    /// <inheritdoc />
    public override string DirectiveName => DirectiveNames.Discover;

    /// <inheritdoc />
    protected override string Namespace => Namespaces.Discovery;

    /// <inheritdoc />
    protected override string EventName => EventNames.DiscoverResponse;

    /// <inheritdoc />
    protected override async Task<(DiscoveryResponsePayload, ContextProperty[]?)> HandleAsync(string? _, DiscoveryDirectivePayload payload, CancellationToken cancellationToken)
    {
        var entities = await client.ListAsync(cancellationToken)
            .Where(w => w.IsExposed(CustomAttributes.Expose))
            .Select(transformer.Transform)
            .Where(w => w?.DisplayCategories.FirstOrDefault() is not null)
            .Cast<DiscoveryEndpoint>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Logger.LogInformation("Discovered {EndpointCount} endpoints", entities.Count);

        return (new() { Endpoints = entities }, default);
    }
}
