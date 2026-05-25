using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.LockController;
using Lando.Alexa.SmartHome.Transformers.Entity;

namespace Lando.Alexa.SmartHome.Discovery.Tests;

/// <summary>
/// Discovery and state coverage for the <c>lock</c> domain.
/// </summary>
public class LockTests
{
    private readonly LockDiscoveryTransformer _discovery = new();
    private readonly LockStateTransformer _state = new();

    [Fact]
    public void Lock_advertises_LockController_and_SmartLock_category()
    {
        var endpoint = _discovery.Transform(TestEntities.Lock());
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.LockController);
        endpoint.DisplayCategories.ShouldContain(DisplayCategory.SmartLock);
    }

    [Fact]
    public void Lock_does_not_advertise_PowerController()
    {
        var endpoint = _discovery.Transform(TestEntities.Lock());

        endpoint.Capabilities.Select(c => c.Interface)
            .ShouldNotContain(Namespaces.PowerController);
    }

    [Fact]
    public void Locked_state_reports_LOCKED()
    {
        var props = _state.Transform(TestEntities.Lock(state: "locked"));

        props.ShouldContain(p =>
            p.Namespace == Namespaces.LockController &&
            p.Name == LockControllerProperties.LockState &&
            p.Value as string == LockState.Locked);
    }

    [Fact]
    public void Unlocked_state_reports_UNLOCKED()
    {
        var props = _state.Transform(TestEntities.Lock(state: "unlocked"));

        props.ShouldContain(p =>
            p.Namespace == Namespaces.LockController &&
            p.Name == LockControllerProperties.LockState &&
            p.Value as string == LockState.Unlocked);
    }

    [Fact]
    public void Jammed_state_reports_JAMMED()
    {
        var props = _state.Transform(TestEntities.Lock(state: "jammed"));

        props.ShouldContain(p =>
            p.Namespace == Namespaces.LockController &&
            p.Name == LockControllerProperties.LockState &&
            p.Value as string == LockState.Jammed);
    }

    /// <summary>
    /// HA emits <c>locking</c> transiently while the motor is running.
    /// Alexa has no in-progress state, so the bridge reports <c>LOCKED</c>
    /// (direction of travel) to avoid a flash of "unlocked" in the app.
    /// </summary>
    [Fact]
    public void Locking_in_progress_reports_LOCKED()
    {
        var props = _state.Transform(TestEntities.Lock(state: "locking"));

        props.ShouldContain(p =>
            p.Namespace == Namespaces.LockController &&
            p.Value as string == LockState.Locked);
    }

    [Fact]
    public void Unlocking_in_progress_reports_UNLOCKED()
    {
        var props = _state.Transform(TestEntities.Lock(state: "unlocking"));

        props.ShouldContain(p =>
            p.Namespace == Namespaces.LockController &&
            p.Value as string == LockState.Unlocked);
    }
}
