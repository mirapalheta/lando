using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Tests.Support;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lando.Alexa.SmartHome.Handlers.Directives.Tests;

/// <summary>
/// <see cref="DiscoverDirectiveHandler"/> projects HA entities into Alexa
/// endpoints. Tests pin the three filtering pivots: exposure, transformer
/// result, and display-category presence.
/// </summary>
public class DiscoverDirectiveHandlerTests
{
    private static readonly IOptions<JsonSerializerOptions> JsonOptions = Options.Create(new JsonSerializerOptions());

    [Theory]
    [MemberData(nameof(EveryDomain))]
    public async Task Surfaces_every_supported_domain(string entityId, HomeAssistantEntity entity)
    {
        var sut = BuildSut(
            [entity],
            e => new DiscoveryEndpoint { EndpointId = e.EntityId.Replace('.', '#'), DisplayCategories = ["OTHER"] });

        var response = await sut.HandleAsync(BuildRequest(), CancellationToken.None);

        var payload = response.Event.Payload.ShouldBeOfType<DiscoveryResponsePayload>();
        payload.Endpoints.ShouldHaveSingleItem();
        payload.Endpoints[0].EndpointId.ShouldBe(entityId.Replace('.', '#'));
    }

    [Fact]
    public async Task Drops_entities_that_are_not_exposed()
    {
        var hidden = TestEntities.Light(entityId: "light.hidden", exposed: false);
        var transformer = new Mock<IEntityTransform<DiscoveryEndpoint>>(MockBehavior.Strict);
        var sut = BuildSut([hidden], transformer);

        var response = await sut.HandleAsync(BuildRequest(), CancellationToken.None);

        response.Event.Payload.ShouldBeOfType<DiscoveryResponsePayload>().Endpoints.ShouldBeEmpty();
        transformer.Verify(t => t.Transform(It.IsAny<HomeAssistantEntity>()), Times.Never);
    }

    [Fact]
    public async Task Drops_entities_whose_transformer_returns_null()
    {
        var entity = TestEntities.Switch(entityId: "switch.unmapped");
        var sut = BuildSut([entity], _ => null);

        var response = await sut.HandleAsync(BuildRequest(), CancellationToken.None);

        response.Event.Payload.ShouldBeOfType<DiscoveryResponsePayload>().Endpoints.ShouldBeEmpty();
    }

    [Fact]
    public async Task Drops_endpoints_with_no_display_category()
    {
        var entity = TestEntities.Light(entityId: "light.no_category");
        var sut = BuildSut(
            [entity],
            e => new DiscoveryEndpoint { EndpointId = e.EntityId, DisplayCategories = [] });

        var response = await sut.HandleAsync(BuildRequest(), CancellationToken.None);

        response.Event.Payload.ShouldBeOfType<DiscoveryResponsePayload>().Endpoints.ShouldBeEmpty();
    }

    [Fact]
    public async Task Preserves_enumeration_order_across_multiple_entities()
    {
        var sut = BuildSut(
            [
                TestEntities.Light(entityId: "light.first"),
                TestEntities.Switch(entityId: "switch.second"),
                TestEntities.Fan(entityId: "fan.third"),
            ],
            e => new DiscoveryEndpoint { EndpointId = e.EntityId, DisplayCategories = ["OTHER"] });

        var response = await sut.HandleAsync(BuildRequest(), CancellationToken.None);

        var endpoints = response.Event.Payload.ShouldBeOfType<DiscoveryResponsePayload>().Endpoints;
        endpoints.Select(e => e.EndpointId).ShouldBe(["light.first", "switch.second", "fan.third"]);
    }

    public static IEnumerable<object[]> EveryDomain() =>
    [
        ["light.living_room", TestEntities.Light()],
        ["switch.outlet", TestEntities.Switch()],
        ["cover.living_room", TestEntities.Cover()],
        ["fan.bedroom", TestEntities.Fan()],
        ["climate.living_room", TestEntities.Climate()],
        ["media_player.living_room_tv", TestEntities.MediaPlayer()],
        ["sensor.living_room_temp", TestEntities.Sensor()],
        ["lock.front_door", TestEntities.Lock()],
    ];

    private static DiscoverDirectiveHandler BuildSut(
        IEnumerable<HomeAssistantEntity> entities,
        Func<HomeAssistantEntity, DiscoveryEndpoint?> transform)
    {
        var transformer = new Mock<IEntityTransform<DiscoveryEndpoint>>();
        transformer.Setup(t => t.Transform(It.IsAny<HomeAssistantEntity>()))
            .Returns<HomeAssistantEntity>(e => transform(e));
        return BuildSut(entities, transformer);
    }

    private static DiscoverDirectiveHandler BuildSut(
        IEnumerable<HomeAssistantEntity> entities,
        Mock<IEntityTransform<DiscoveryEndpoint>> transformer)
    {
        var client = new Mock<IHomeAssistantClient>();
        client.Setup(c => c.ListAsync(It.IsAny<CancellationToken>())).Returns(ToAsync(entities));
        return new DiscoverDirectiveHandler(client.Object, transformer.Object, validator: null!, JsonOptions,
            NullLogger<DiscoverDirectiveHandler>.Instance);
    }

    private static async IAsyncEnumerable<HomeAssistantEntity> ToAsync(
        IEnumerable<HomeAssistantEntity> entities,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var e in entities)
        { ct.ThrowIfCancellationRequested(); yield return e; await Task.Yield(); }
    }

    private static Request BuildRequest()
        => RequestFixtures.Directive(Namespaces.Discovery, DirectiveNames.Discover,
            payload: new DiscoveryDirectivePayload());
}
