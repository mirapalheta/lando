using Lando.Alexa.SmartHome.Models.Core;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// <see cref="TurnOnPayloadTransform"/> and <see cref="TurnOffPayloadTransform"/>
/// share a cover-vs-everything-else branching pattern: covers route to
/// <c>open_cover</c>/<c>close_cover</c>, all other domains to <c>turn_on</c>/<c>turn_off</c>.
/// The tests below pin both branches per directive — drift in either direction
/// silently breaks cover entities (HA's <c>cover.turn_on</c> service exists on
/// some integrations but not all).
/// </summary>
public class PowerControllerTransformTests
{
    [Theory]
    [InlineData("light.kitchen", "turn_on")]
    [InlineData("switch.outlet", "turn_on")]
    [InlineData("fan.bedroom", "turn_on")]
    [InlineData("climate.living_room", "turn_on")]
    [InlineData("media_player.tv", "turn_on")]
    [InlineData("cover.shade", "open_cover")]
    public void TurnOn_routes_to_expected_service(string entityId, string expectedService)
    {
        var entity = TestEntities.From(entityId);
        var request = new TurnOnPayloadTransform().Transform(entity, EmptyPayload.Instance);

        request.EntityId.ShouldBe(entityId);
        request.Service.ShouldBe(expectedService);
    }

    [Theory]
    [InlineData("light.kitchen", "turn_off")]
    [InlineData("switch.outlet", "turn_off")]
    [InlineData("fan.bedroom", "turn_off")]
    [InlineData("climate.living_room", "turn_off")]
    [InlineData("media_player.tv", "turn_off")]
    [InlineData("cover.shade", "close_cover")]
    public void TurnOff_routes_to_expected_service(string entityId, string expectedService)
    {
        var entity = TestEntities.From(entityId);
        var request = new TurnOffPayloadTransform().Transform(entity, EmptyPayload.Instance);

        request.EntityId.ShouldBe(entityId);
        request.Service.ShouldBe(expectedService);
    }
}
