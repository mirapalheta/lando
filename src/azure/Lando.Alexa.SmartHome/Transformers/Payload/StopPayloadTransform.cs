using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Payload;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Handles the <c>Alexa.PlaybackController.Stop</c> directive by calling
/// <c>media_player.media_stop</c> on the HA entity.
/// </summary>
/// <remarks>
/// PlaybackController is only advertised on media players, and only with the
/// operations their <c>supported_features</c> allow; other domains are rejected.
/// </remarks>
public class StopPayloadTransform : IPayloadTransform<EmptyPayload>
{
    /// <inheritdoc />
    public HomeAssistantRequest Transform(HomeAssistantEntity entity, EmptyPayload payload)
        => entity.GetDomain() switch
        {
            Domains.MediaPlayer => HomeAssistantRequest.MediaStop(entity.EntityId),
            _ => throw new AlexaSmartHomeException(ErrorType.InvalidDirective, $"Entity {entity.EntityId} does not support playback control")
        };
}
