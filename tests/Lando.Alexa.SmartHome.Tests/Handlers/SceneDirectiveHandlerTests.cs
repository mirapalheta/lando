using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.ChangeReport;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.SceneController;
using Lando.Alexa.SmartHome.Tests.Support;
using Lando.Alexa.SmartHome.Transformers.Payload;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives.Tests;

/// <summary>
/// <see cref="SceneDirectiveHandler"/> reuses the <see cref="ControlDirectiveHandler{TRequest,TResponse}"/>
/// dispatch flow but answers in the SceneController namespace. Activate/Deactivate
/// reuse the existing <c>TurnOn</c>/<c>TurnOff</c> payload transforms (domain
/// derived from the entity id, so scenes and scripts share one handler). These
/// tests pin the namespace/event/cause of the response, the underlying service
/// call, and the missing-entity / downstream-failure error mapping.
/// </summary>
public class SceneDirectiveHandlerTests
{
    private static readonly IOptions<JsonSerializerOptions> JsonOptions = Options.Create(new JsonSerializerOptions());

    [Fact]
    public async Task Activate_runs_script_turn_on_and_returns_ActivationStarted()
    {
        const string entityId = "script.wake_up";
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.GetAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestEntities.Script(entityId: entityId));
        var sut = BuildSut(client.Object, DirectiveNames.Activate, EventNames.ActivationStarted, new TurnOnPayloadTransform());

        var response = await sut.HandleAsync(Request(DirectiveNames.Activate, "script#wake_up"), CancellationToken.None);

        response.Event.Header.Namespace.ShouldBe(Namespaces.SceneController);
        response.Event.Header.Name.ShouldBe(EventNames.ActivationStarted);
        response.Event.Payload.ShouldBeOfType<SceneActivationPayload>()
            .Cause.Type.ShouldBe(ChangeCauseType.VoiceInteraction);
        client.Verify(c => c.CallServiceAsync(It.Is<HomeAssistantRequest>(r =>
            r.EntityId == entityId && r.Service == "turn_on"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_activates_scene_via_turn_on()
    {
        const string entityId = "scene.movie_night";
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.GetAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestEntities.Scene(entityId: entityId));
        var sut = BuildSut(client.Object, DirectiveNames.Activate, EventNames.ActivationStarted, new TurnOnPayloadTransform());

        await sut.HandleAsync(Request(DirectiveNames.Activate, "scene#movie_night"), CancellationToken.None);

        client.Verify(c => c.CallServiceAsync(It.Is<HomeAssistantRequest>(r =>
            r.EntityId == entityId && r.Service == "turn_on"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_stops_script_via_turn_off_and_returns_DeactivationStarted()
    {
        const string entityId = "script.wake_up";
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.GetAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestEntities.Script(entityId: entityId));
        var sut = BuildSut(client.Object, DirectiveNames.Deactivate, EventNames.DeactivationStarted, new TurnOffPayloadTransform());

        var response = await sut.HandleAsync(Request(DirectiveNames.Deactivate, "script#wake_up"), CancellationToken.None);

        response.Event.Header.Namespace.ShouldBe(Namespaces.SceneController);
        response.Event.Header.Name.ShouldBe(EventNames.DeactivationStarted);
        client.Verify(c => c.CallServiceAsync(It.Is<HomeAssistantRequest>(r =>
            r.EntityId == entityId && r.Service == "turn_off"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Throws_NoSuchEndpoint_when_entity_is_missing()
    {
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HomeAssistantEntity?)null);
        var sut = BuildSut(client.Object, DirectiveNames.Activate, EventNames.ActivationStarted, new TurnOnPayloadTransform());

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(Request(DirectiveNames.Activate, "script#missing"), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.NoSuchEndpoint);
    }

    [Fact]
    public async Task Wraps_downstream_failure_as_EndpointUnreachable()
    {
        const string entityId = "script.wake_up";
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.GetAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestEntities.Script(entityId: entityId));
        client.Setup(c => c.CallServiceAsync(It.IsAny<HomeAssistantRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("502 bad gateway"));
        var sut = BuildSut(client.Object, DirectiveNames.Activate, EventNames.ActivationStarted, new TurnOnPayloadTransform());

        var ex = await Should.ThrowAsync<AlexaSmartHomeException>(
            () => sut.HandleAsync(Request(DirectiveNames.Activate, "script#wake_up"), CancellationToken.None));
        ex.Error.ShouldBe(ErrorType.EndpointUnreachable);
    }

    private static SceneDirectiveHandler BuildSut(
        IHomeAssistantClient client, string directiveName, string eventName, IPayloadTransform<EmptyPayload> transform)
    {
        var services = new ServiceCollection();
        services.AddSingleton(client);
        services.AddKeyedSingleton(directiveName, transform);
        return new SceneDirectiveHandler(
            services.BuildServiceProvider(), directiveName, eventName, validator: null!, JsonOptions,
            NullLogger<SceneDirectiveHandler>.Instance);
    }

    private static Request Request(string directiveName, string endpointId)
        => RequestFixtures.Directive(
            Namespaces.SceneController, directiveName, payload: new { },
            endpoint: RequestFixtures.Endpoint(endpointId: endpointId));
}
