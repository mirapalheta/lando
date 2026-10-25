using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.ErrorResponse;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// PlaybackController transforms — one HA media_player service per directive,
/// only legal on media_player entities.
/// </summary>
public class PlaybackControllerTransformTests
{
    public static IEnumerable<object[]> Transforms() =>
    [
        [new PlayPayloadTransform(), "media_play"],
        [new PausePayloadTransform(), "media_pause"],
        [new StopPayloadTransform(), "media_stop"],
        [new NextPayloadTransform(), "media_next_track"],
        [new PreviousPayloadTransform(), "media_previous_track"],
    ];

    [Theory]
    [MemberData(nameof(Transforms))]
    public void Maps_directive_to_media_player_service(IPayloadTransform<EmptyPayload> transform, string service)
    {
        var request = transform.Transform(TestEntities.MediaPlayer(), EmptyPayload.Instance);

        request.Service.ShouldBe(service);
        request.EntityId.ShouldBe("media_player.living_room_tv");
        request.Repeat.ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(Transforms))]
    public void Non_media_player_throws_InvalidDirective(IPayloadTransform<EmptyPayload> transform, string _)
    {
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => transform.Transform(TestEntities.Light(), EmptyPayload.Instance));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }
}
