using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.StepSpeaker;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// StepSpeaker transforms — only legal on media_player entities. AdjustVolume
/// maps the signed step count onto repeated volume_up / volume_down calls, and
/// collapses Alexa's default (±10 when no amount was spoken) to a single step.
/// </summary>
public class StepSpeakerControllerTransformTests
{
    [Theory]
    [InlineData(3, "volume_up", 3)]
    [InlineData(1, "volume_up", 1)]
    [InlineData(-1, "volume_down", 1)]
    [InlineData(-5, "volume_down", 5)]
    public void AdjustVolume_repeats_step_service_per_explicit_step(int steps, string service, int repeat)
    {
        var request = new StepSpeakerAdjustVolumePayloadTransform().Transform(
            TestEntities.MediaPlayer(), new StepSpeakerAdjustVolumePayload { VolumeSteps = steps, VolumeStepsDefault = false });

        request.Service.ShouldBe(service);
        request.Repeat.ShouldBe(repeat);
        request.VolumeLevel.ShouldBeNull();
    }

    [Theory]
    [InlineData(10, "volume_up")]
    [InlineData(-10, "volume_down")]
    public void AdjustVolume_default_collapses_to_one_step(int steps, string service)
    {
        var request = new StepSpeakerAdjustVolumePayloadTransform().Transform(
            TestEntities.MediaPlayer(), new StepSpeakerAdjustVolumePayload { VolumeSteps = steps, VolumeStepsDefault = true });

        request.Service.ShouldBe(service);
        request.Repeat.ShouldBe(1);
    }

    [Fact]
    public void AdjustVolume_with_missing_default_flag_honours_the_count()
    {
        var request = new StepSpeakerAdjustVolumePayloadTransform().Transform(
            TestEntities.MediaPlayer(), new StepSpeakerAdjustVolumePayload { VolumeSteps = 4 });

        request.Repeat.ShouldBe(4);
    }

    [Fact]
    public void AdjustVolume_on_non_media_player_throws_InvalidDirective()
    {
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new StepSpeakerAdjustVolumePayloadTransform().Transform(TestEntities.Light(), new StepSpeakerAdjustVolumePayload { VolumeSteps = 1 }));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetMute_forwards_the_bool(bool mute)
    {
        var request = new StepSpeakerSetMutePayloadTransform().Transform(
            TestEntities.MediaPlayer(), new StepSpeakerSetMutePayload { Mute = mute });

        request.Service.ShouldBe("volume_mute");
        request.IsVolumeMuted.ShouldBe(mute);
    }

    [Fact]
    public void SetMute_on_non_media_player_throws_InvalidDirective()
    {
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new StepSpeakerSetMutePayloadTransform().Transform(TestEntities.Light(), new StepSpeakerSetMutePayload { Mute = true }));
        ex.Error.ShouldBe(ErrorType.InvalidDirective);
    }
}
