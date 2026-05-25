using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// Speaker controller transforms — only legal on media_player entities. The
/// unit conversion is the load-bearing detail: Alexa sends volume 0..100,
/// HA expects 0.0..1.0 on volume_set. AdjustVolume reads the current and
/// adds the (scaled) delta with a 0..1 clamp.
/// </summary>
public class SpeakerControllerTransformTests
{
    [Theory]
    [InlineData(0, 0d)]
    [InlineData(50, 0.5)]
    [InlineData(100, 1.0)]
    [InlineData(120, 1.0)]   // clamped at 100
    [InlineData(-5, 0d)]      // clamped at 0
    public void SetVolume_scales_to_0_to_1_volume_level(int alexa, double expectedHa)
    {
        var entity = TestEntities.MediaPlayer();
        var request = new SetVolumePayloadTransform().Transform(entity, new SetVolumePayload { Volume = alexa });

        request.Service.ShouldBe("volume_set");
        request.VolumeLevel.ShouldBe(expectedHa);
    }

    [Fact]
    public void SetVolume_on_non_media_player_throws_InvalidDirective()
    {
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new SetVolumePayloadTransform().Transform(TestEntities.Light(), new SetVolumePayload { Volume = 50 }));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }

    [Theory]
    [InlineData(0.5, 20, 0.7)]
    [InlineData(0.5, -25, 0.25)]
    [InlineData(0.9, 50, 1.0)]    // clamp at 1.0
    [InlineData(0.1, -50, 0d)]     // clamp at 0.0
    public void AdjustVolume_scales_and_clamps(double currentHa, int delta, double expectedHa)
    {
        var entity = TestEntities.MediaPlayer(volumeLevel: currentHa);
        var request = new AdjustVolumePayloadTransform().Transform(entity, new AdjustVolumePayload { Volume = delta });

        request.Service.ShouldBe("volume_set");
        request.VolumeLevel!.Value.ShouldBe(expectedHa, tolerance: 0.001);
    }

    [Fact]
    public void AdjustVolume_on_non_media_player_throws_InvalidDirective()
    {
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new AdjustVolumePayloadTransform().Transform(TestEntities.Light(), new AdjustVolumePayload { Volume = 10 }));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetMute_forwards_the_bool(bool mute)
    {
        var entity = TestEntities.MediaPlayer();
        var request = new SetMutePayloadTransform().Transform(entity, new SetMutePayload { Mute = mute });

        request.Service.ShouldBe("volume_mute");
        request.IsVolumeMuted.ShouldBe(mute);
    }

    [Fact]
    public void SetMute_on_non_media_player_throws_InvalidDirective()
    {
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new SetMutePayloadTransform().Transform(TestEntities.Light(), new SetMutePayload { Mute = true }));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }
}
