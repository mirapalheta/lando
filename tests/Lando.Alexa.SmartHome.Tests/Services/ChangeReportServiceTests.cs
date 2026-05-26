using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant;
using Lando.HomeAssistant.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lando.Alexa.SmartHome.Services.Tests;

/// <summary>
/// <see cref="ChangeReportService"/> bridges HA WebSocket <c>state_changed</c>
/// events to the Alexa Event Gateway. Tests pin: exposure filter, diff
/// against the prior snapshot, endpoint-id separator translation, and the
/// no-change short-circuit.
/// </summary>
public class ChangeReportServiceTests
{
    [Fact]
    public async Task First_seen_entity_reports_every_property_as_changed()
    {
        var newProps = new[] { Property("powerState", "ON") };
        var entity = TestEntities.Light(entityId: "light.kitchen");
        var ev = new HomeAssistantStateChangedEvent { EntityId = "light.kitchen", NewState = entity, OldState = null };

        var gateway = await RunOnceAsync(ev, transform: _ => newProps);

        gateway.Calls.ShouldHaveSingleItem();
        gateway.Calls[0].EndpointId.ShouldBe("light#kitchen");
        gateway.Calls[0].Changed.ShouldBe(newProps);
        gateway.Calls[0].All.ShouldBe(newProps);
    }

    [Fact]
    public async Task Diff_reports_only_properties_that_moved()
    {
        var oldEntity = TestEntities.Light(entityId: "light.kitchen", state: "off");
        var newEntity = TestEntities.Light(entityId: "light.kitchen", state: "on");
        var ev = new HomeAssistantStateChangedEvent { EntityId = "light.kitchen", NewState = newEntity, OldState = oldEntity };

        var gateway = await RunOnceAsync(ev, transform: e => e == newEntity
            ? [Property("powerState", "ON"), Property("brightness", 75)]
            : [Property("powerState", "OFF"), Property("brightness", 75)]);

        var call = gateway.Calls.ShouldHaveSingleItem();
        call.Changed.Length.ShouldBe(1);
        call.Changed[0].Name.ShouldBe("powerState");
        call.All.Length.ShouldBe(2);
    }

    [Fact]
    public async Task Skips_when_no_alexa_visible_property_changed()
    {
        var oldEntity = TestEntities.Light(entityId: "light.kitchen");
        var newEntity = TestEntities.Light(entityId: "light.kitchen");
        var ev = new HomeAssistantStateChangedEvent { EntityId = "light.kitchen", NewState = newEntity, OldState = oldEntity };

        var gateway = await RunOnceAsync(ev, transform: _ => [Property("powerState", "ON")]);

        gateway.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Skips_when_new_state_is_not_exposed()
    {
        var newEntity = TestEntities.Light(entityId: "light.kitchen", exposed: false);
        var ev = new HomeAssistantStateChangedEvent { EntityId = "light.kitchen", NewState = newEntity, OldState = null };

        var gateway = await RunOnceAsync(ev, transform: _ => [Property("powerState", "ON")]);

        gateway.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Endpoint_id_uses_alexa_separator()
    {
        var entity = TestEntities.Switch(entityId: "switch.back_bedroom_lights");
        var ev = new HomeAssistantStateChangedEvent { EntityId = entity.EntityId, NewState = entity, OldState = null };

        var gateway = await RunOnceAsync(ev, transform: _ => [Property("powerState", "ON")]);

        gateway.Calls.ShouldHaveSingleItem().EndpointId.ShouldBe("switch#back_bedroom_lights");
    }

    private record GatewayCall(string EndpointId, ContextProperty[] Changed, ContextProperty[] All);

    private class GatewayRecorder
    {
        public List<GatewayCall> Calls { get; } = new();
    }

    /// <summary>
    /// Runs the service over a single-event subscription. Waits for the
    /// subscription enumerable to be fully consumed (i.e. the event has been
    /// processed) before asserting, rather than relying on a fixed delay that
    /// is fragile under CI scheduler pressure.
    /// </summary>
    private static async Task<GatewayRecorder> RunOnceAsync(
        HomeAssistantStateChangedEvent ev,
        Func<HomeAssistantEntity, ContextProperty[]?> transform)
    {
        var subscriptionDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var ws = new Mock<IHomeAssistantWebSocketClient>();
        ws.Setup(w => w.SubscribeAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => ToAsync([ev], subscriptionDone, ct));

        var recorder = new GatewayRecorder();
        var gateway = new Mock<IEventGatewayClient>();
        gateway.Setup(g => g.SendChangeReportAsync(
                It.IsAny<string>(),
                It.IsAny<ContextProperty[]>(),
                It.IsAny<ContextProperty[]>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, ContextProperty[], ContextProperty[], CancellationToken>(
                (endpointId, changed, all, _) =>
                {
                    recorder.Calls.Add(new GatewayCall(endpointId, changed, all));
                    return new ValueTask<bool>(true);
                });

        var transformer = new Mock<IEntityTransform<ContextProperty[]>>();
        transformer.Setup(t => t.Transform(It.IsAny<HomeAssistantEntity>()))
            .Returns(transform);

        var sut = new ChangeReportService(ws.Object, gateway.Object, transformer.Object,
            NullLogger<ChangeReportService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await subscriptionDone.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);

        return recorder;
    }

    private static async IAsyncEnumerable<HomeAssistantStateChangedEvent> ToAsync(
        IEnumerable<HomeAssistantStateChangedEvent> events,
        TaskCompletionSource done,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var e in events)
        {
            ct.ThrowIfCancellationRequested();
            yield return e;
            await Task.Yield();
        }
        done.TrySetResult();
    }

    private static ContextProperty Property(string name, object value) => new()
    {
        Namespace = Namespaces.PowerController,
        Name = name,
        Value = value,
        TimeOfSample = DateTime.UtcNow,
    };
}
