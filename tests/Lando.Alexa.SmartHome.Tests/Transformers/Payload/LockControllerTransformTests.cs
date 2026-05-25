using Lando.Alexa.SmartHome.Models.Core;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// LockController transforms are 1:1 with HA's <c>lock.lock</c> /
/// <c>lock.unlock</c> services. Pinned here so a future contributor can't
/// accidentally route through <c>turn_on</c>/<c>turn_off</c> (which HA also
/// accepts on locks but with different semantics).
/// </summary>
public class LockControllerTransformTests
{
    [Fact]
    public void Lock_emits_lock_service()
    {
        var entity = TestEntities.Lock();
        var request = new LockPayloadTransform().Transform(entity, EmptyPayload.Instance);

        request.Service.ShouldBe("lock");
        request.EntityId.ShouldBe(entity.EntityId);
    }

    [Fact]
    public void Unlock_emits_unlock_service()
    {
        var entity = TestEntities.Lock();
        var request = new UnlockPayloadTransform().Transform(entity, EmptyPayload.Instance);

        request.Service.ShouldBe("unlock");
        request.EntityId.ShouldBe(entity.EntityId);
    }
}
