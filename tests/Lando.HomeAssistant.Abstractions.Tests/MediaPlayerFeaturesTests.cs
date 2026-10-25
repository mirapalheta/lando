namespace Lando.HomeAssistant.Tests;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Pins <see cref="MediaPlayerFeatures"/> to HA's <c>MediaPlayerEntityFeature</c>
/// values. HA skips bit 6, so a contiguous enumeration silently mis-gates every
/// feature from TURN_ON up — which is how VOLUME_STEP/STOP/PLAY went unseen on
/// step-only TVs.
/// </summary>
public class MediaPlayerFeaturesTests
{
    [Theory]
    [InlineData(MediaPlayerFeatures.Pause, 1)]
    [InlineData(MediaPlayerFeatures.Seek, 2)]
    [InlineData(MediaPlayerFeatures.VolumeSet, 4)]
    [InlineData(MediaPlayerFeatures.VolumeMute, 8)]
    [InlineData(MediaPlayerFeatures.PreviousTrack, 16)]
    [InlineData(MediaPlayerFeatures.NextTrack, 32)]
    [InlineData(MediaPlayerFeatures.TurnOn, 128)]
    [InlineData(MediaPlayerFeatures.TurnOff, 256)]
    [InlineData(MediaPlayerFeatures.PlayMedia, 512)]
    [InlineData(MediaPlayerFeatures.VolumeStep, 1024)]
    [InlineData(MediaPlayerFeatures.SelectSource, 2048)]
    [InlineData(MediaPlayerFeatures.Stop, 4096)]
    [InlineData(MediaPlayerFeatures.ClearPlaylist, 8192)]
    [InlineData(MediaPlayerFeatures.Play, 16384)]
    [InlineData(MediaPlayerFeatures.ShuffleSet, 32768)]
    [InlineData(MediaPlayerFeatures.SelectSoundMode, 65536)]
    [InlineData(MediaPlayerFeatures.BrowseMedia, 131072)]
    [InlineData(MediaPlayerFeatures.RepeatSet, 262144)]
    [InlineData(MediaPlayerFeatures.Grouping, 524288)]
    public void Feature_bits_match_Home_Assistant(int actual, int expected)
        => actual.ShouldBe(expected);

    /// <summary>
    /// A real <c>androidtv_remote</c> TV (153529) decodes to exactly the features
    /// HA shows for it.
    /// </summary>
    [Fact]
    public void Android_TV_remote_bitmask_decodes_to_its_features()
    {
        const int features = 153529;

        (MediaPlayerFeatures.Pause | MediaPlayerFeatures.VolumeMute | MediaPlayerFeatures.PreviousTrack
            | MediaPlayerFeatures.NextTrack | MediaPlayerFeatures.TurnOn | MediaPlayerFeatures.TurnOff
            | MediaPlayerFeatures.PlayMedia | MediaPlayerFeatures.VolumeStep | MediaPlayerFeatures.Stop
            | MediaPlayerFeatures.Play | MediaPlayerFeatures.BrowseMedia).ShouldBe(features);
        (features & MediaPlayerFeatures.VolumeSet).ShouldBe(0);
        (features & MediaPlayerFeatures.SelectSource).ShouldBe(0);
    }
}
