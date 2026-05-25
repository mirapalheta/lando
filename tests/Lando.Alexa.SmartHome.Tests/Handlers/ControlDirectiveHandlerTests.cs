using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Tests.Support;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives.Tests;

/// <summary>
/// <see cref="ControlDirectiveHandler{TRequest}"/> turns control directives
/// into HA service calls via a keyed <see cref="IPayloadTransform{TPayload}"/>.
/// These tests pin happy-path dispatch, missing-endpoint/missing-entity errors,
/// generic-failure wrapping, and cancellation propagation.
/// </summary>
public class ControlDirectiveHandlerTests
{
    private const string DirectiveName = DirectiveNames.TurnOn;
    private const string EntityId = "light.kitchen";
    private const string EndpointId = "light#kitchen";

    private static readonly IOptions<JsonSerializerOptions> JsonOptions = Options.Create(new JsonSerializerOptions());

    [Fact]
    public async Task Dispatches_payload_transform_and_returns_empty_response()
    {
        var entity = TestEntities.Light(entityId: EntityId);
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.GetAsync(EntityId, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var transform = new Mock<IPayloadTransform<EmptyPayload>>();
        transform.Setup(t => t.Transform(entity, It.IsAny<EmptyPayload>()))
            .Returns(HomeAssistantRequest.TurnOn(EntityId));
        var sut = BuildSut(client.Object, transform.Object);

        var response = await sut.HandleAsync(BuildRequest(), CancellationToken.None);

        response.Event.Header.Namespace.ShouldBe(Namespaces.Alexa);
        response.Event.Header.Name.ShouldBe(EventNames.Response);
        client.Verify(c => c.CallServiceAsync(It.Is<HomeAssistantRequest>(r =>
            r.EntityId == EntityId && r.Service == "turn_on"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Throws_InvalidDirective_when_endpoint_is_missing()
    {
        var sut = BuildSut(new Mock<IHomeAssistantClient>().Object, new Mock<IPayloadTransform<EmptyPayload>>().Object);
        var request = RequestFixtures.Directive(Namespaces.PowerController, DirectiveName, payload: new { });

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(request, CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }

    [Fact]
    public async Task Throws_NoSuchEndpoint_when_entity_is_missing()
    {
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.GetAsync(EntityId, It.IsAny<CancellationToken>())).ReturnsAsync((HomeAssistantEntity?)null);
        var sut = BuildSut(client.Object, new Mock<IPayloadTransform<EmptyPayload>>().Object);

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.NoSuchEndpoint);
    }

    [Fact]
    public async Task Wraps_downstream_exception_as_EndpointUnreachable()
    {
        var entity = TestEntities.Light(entityId: EntityId);
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.GetAsync(EntityId, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        client.Setup(c => c.CallServiceAsync(It.IsAny<HomeAssistantRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("502 bad gateway"));
        var transform = new Mock<IPayloadTransform<EmptyPayload>>();
        transform.Setup(t => t.Transform(It.IsAny<HomeAssistantEntity>(), It.IsAny<EmptyPayload>()))
            .Returns(HomeAssistantRequest.TurnOn(EntityId));
        var sut = BuildSut(client.Object, transform.Object);

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(BuildRequest(), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.EndpointUnreachable);
    }

    [Fact]
    public async Task Propagates_OperationCanceledException_unwrapped()
    {
        var entity = TestEntities.Light(entityId: EntityId);
        var client = new Mock<IHomeAssistantClient>();
        using var cts = new CancellationTokenSource();
        client.Setup(c => c.GetAsync(EntityId, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        client.Setup(c => c.CallServiceAsync(It.IsAny<HomeAssistantRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));
        var transform = new Mock<IPayloadTransform<EmptyPayload>>();
        transform.Setup(t => t.Transform(It.IsAny<HomeAssistantEntity>(), It.IsAny<EmptyPayload>()))
            .Returns(HomeAssistantRequest.TurnOn(EntityId));
        var sut = BuildSut(client.Object, transform.Object);

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.HandleAsync(BuildRequest(), cts.Token));
    }

    private static ControlDirectiveHandler<EmptyPayload> BuildSut(
        IHomeAssistantClient client, IPayloadTransform<EmptyPayload> transform)
    {
        var services = new ServiceCollection();
        services.AddSingleton(client);
        services.AddKeyedSingleton(DirectiveName, transform);
        return new ControlDirectiveHandler<EmptyPayload>(
            services.BuildServiceProvider(), DirectiveName, validator: null!, JsonOptions,
            NullLogger<ControlDirectiveHandler<EmptyPayload>>.Instance);
    }

    private static Request BuildRequest()
        => RequestFixtures.Directive(
            Namespaces.PowerController, DirectiveName, payload: new { },
            endpoint: RequestFixtures.Endpoint(endpointId: EndpointId));
}
