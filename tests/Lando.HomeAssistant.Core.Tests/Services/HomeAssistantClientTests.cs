using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lando.HomeAssistant.Models;

namespace Lando.HomeAssistant.Services.Tests;

/// <summary>
/// <see cref="HomeAssistantClient"/> is pure composition of
/// <see cref="IDeviceDiscovery"/> + <see cref="IServiceCaller"/>. Tests pin
/// that each method delegates to exactly one collaborator and forwards
/// arguments verbatim.
/// </summary>
public class HomeAssistantClientTests
{
    [Fact]
    public async Task GetAsync_delegates_to_DeviceDiscovery()
    {
        var entity = new HomeAssistantEntity { EntityId = "light.kitchen", State = "on" };
        var discovery = new Mock<IDeviceDiscovery>();
        discovery.Setup(d => d.GetAsync("light.kitchen", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var caller = new Mock<IServiceCaller>(MockBehavior.Strict);
        var sut = new HomeAssistantClient(discovery.Object, caller.Object);

        var result = await sut.GetAsync("light.kitchen", CancellationToken.None);

        result.ShouldBeSameAs(entity);
        discovery.Verify(d => d.GetAsync("light.kitchen", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_delegates_to_DeviceDiscovery()
    {
        var entities = new[]
        {
            new HomeAssistantEntity { EntityId = "light.kitchen", State = "on" },
            new HomeAssistantEntity { EntityId = "switch.outlet", State = "off" },
        };
        var discovery = new Mock<IDeviceDiscovery>();
        discovery.Setup(d => d.ListAsync(It.IsAny<CancellationToken>())).Returns(ToAsync(entities));
        var sut = new HomeAssistantClient(discovery.Object, new Mock<IServiceCaller>().Object);

        var ids = new List<string>();
        await foreach (var e in sut.ListAsync(CancellationToken.None))
            ids.Add(e.EntityId);

        ids.ShouldBe(["light.kitchen", "switch.outlet"]);
    }

    [Fact]
    public async Task CallServiceAsync_delegates_to_ServiceCaller()
    {
        var caller = new Mock<IServiceCaller>();
        caller.Setup(c => c.CallServiceAsync(It.IsAny<HomeAssistantRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = new HomeAssistantClient(new Mock<IDeviceDiscovery>().Object, caller.Object);
        var request = HomeAssistantRequest.TurnOn("light.kitchen");

        await sut.CallServiceAsync(request, CancellationToken.None);

        caller.Verify(c => c.CallServiceAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<HomeAssistantEntity> ToAsync(
        IEnumerable<HomeAssistantEntity> entities,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var e in entities)
        { ct.ThrowIfCancellationRequested(); yield return e; await Task.Yield(); }
    }
}
