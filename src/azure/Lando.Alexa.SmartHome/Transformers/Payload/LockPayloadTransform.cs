using Lando.Alexa.SmartHome.Models.Core;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

/// <summary>
/// Handles the <c>Alexa.LockController.Lock</c> directive by calling
/// <c>lock.lock</c> on the HA entity.
/// </summary>
public class LockPayloadTransform : IPayloadTransform<EmptyPayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, EmptyPayload payload)
        => HomeAssistantRequest.Lock(entity.EntityId);
}
