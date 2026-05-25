using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.Alexa.SmartHome.Tests.Support;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives.Tests;

/// <summary>
/// <see cref="ReportStateDirectiveHandler"/> answers Alexa's
/// "what's the current state of this endpoint?" with a snapshot of every
/// reportable property. Tests pin: happy path; missing endpoint;
/// unexposed/missing entity; null-transformer fallback.
/// </summary>
public class ReportStateDirectiveHandlerTests
{
    private const string EntityId = "light.kitchen";
    private const string EndpointId = "light#kitchen";

    private static readonly IOptions<JsonSerializerOptions> JsonOptions = Options.Create(new JsonSerializerOptions());

    [Fact]
    public async Task Returns_StateReport_with_transformed_properties()
    {
        var sut = BuildSut(
            TestEntities.Light(entityId: EntityId),
            _ =>
            [
                new()
                {
                    Namespace = Namespaces.PowerController,
                    Name = "powerState",
                    Value = PowerState.On,
                    TimeOfSample = DateTime.UtcNow,
                },
            ]);

        var response = await sut.HandleAsync(BuildRequest(), CancellationToken.None);

        response.Event.Header.Namespace.ShouldBe(Namespaces.Alexa);
        response.Event.Header.Name.ShouldBe(EventNames.StateReport);
        response.Context.ShouldNotBeNull();
        response.Context!.Properties.ShouldHaveSingleItem().Namespace.ShouldBe(Namespaces.PowerController);
    }

    [Fact]
    public async Task Throws_InvalidDirective_when_endpoint_is_missing()
    {
        var sut = BuildSut(entity: null, _ => Array.Empty<ContextProperty>());
        var request = RequestFixtures.Directive(Namespaces.Alexa, DirectiveNames.ReportState, payload: new { });

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(request, CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }

    [Fact]
    public async Task Throws_NoSuchEndpoint_when_entity_is_not_exposed()
    {
        var entity = TestEntities.Light(entityId: EntityId, exposed: false);
        var sut = BuildSut(entity, _ => Array.Empty<ContextProperty>());

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.NoSuchEndpoint);
    }

    [Fact]
    public async Task Throws_NoSuchEndpoint_when_entity_is_missing()
    {
        var sut = BuildSut(entity: null, _ => Array.Empty<ContextProperty>());

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.NoSuchEndpoint);
    }

    [Fact]
    public async Task Throws_NoSuchEndpoint_when_transformer_returns_null()
    {
        var entity = TestEntities.Light(entityId: EntityId);
        var sut = BuildSut(entity, _ => null);

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.NoSuchEndpoint);
    }

    private static ReportStateDirectiveHandler BuildSut(
        HomeAssistantEntity? entity,
        Func<HomeAssistantEntity, ContextProperty[]?> transform)
    {
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.GetAsync(EntityId, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var transformer = new Mock<IEntityTransform<ContextProperty[]>>();
        transformer.Setup(t => t.Transform(It.IsAny<HomeAssistantEntity>()))
            .Returns<HomeAssistantEntity>(e => transform(e));
        return new ReportStateDirectiveHandler(client.Object, transformer.Object, validator: null!, JsonOptions,
            NullLogger<ReportStateDirectiveHandler>.Instance);
    }

    private static Request BuildRequest()
        => RequestFixtures.Directive(Namespaces.Alexa, DirectiveNames.ReportState,
            payload: new { }, endpoint: RequestFixtures.Endpoint(endpointId: EndpointId));
}
