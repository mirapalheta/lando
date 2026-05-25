using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

/// <summary>
/// Handles the <c>Alexa.LockController.Unlock</c> directive by calling
/// <c>lock.unlock</c> on the HA entity.
/// </summary>
public class UnlockPayloadTransform : IPayloadTransform<EmptyPayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, EmptyPayload payload)
        => HomeAssistantRequest.Unlock(entity.EntityId);
}
