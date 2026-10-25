using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.PlaybackStateReporter;
using Lando.Alexa.SmartHome.Transformers.Entity;

namespace Lando.Alexa.SmartHome.Discovery.Tests;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Media player discovery emits PowerController unconditionally, then Speaker
/// (VOLUME_SET) or StepSpeaker (VOLUME_STEP without VOLUME_SET), and a
/// PlaybackController whose operations mirror the playback feature bits,
/// paired with a PlaybackStateReporter.
/// Channel and Input controllers are intentionally not advertised.
/// </summary>
/// <remarks>
/// Every capability is gated on <c>supported_features</c> so Alexa is only
/// offered directives the integration can honour; the 153529 fixture is the
/// live shape of the house's <c>androidtv_remote</c> TVs.
/// </remarks>
public class MediaPlayerDiscoveryTests
{
    private readonly MediaPlayerDiscoveryTransformer _transformer = new();

    /// <summary>
    /// Asserts that a default media_player (a TV with VOLUME_SET) advertises
    /// both PowerController and Speaker.
    /// </summary>
    /// <remarks>
    /// Default fixture matches the common HA TV integration shape — the
    /// assertion pins both capabilities to catch regressions in either branch
    /// independently.
    /// </remarks>
    [Fact]
    public void Default_tv_advertises_power_and_speaker()
    {
        var endpoint = _transformer.Transform(TestEntities.MediaPlayer());
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldContain(Namespaces.Speaker);
    }

    /// <summary>
    /// Asserts that Speaker is dropped when the entity's
    /// <c>supported_features</c> bitmask omits <c>VOLUME_SET</c>.
    /// </summary>
    /// <remarks>
    /// Some media_player integrations expose only on/off (for example simple
    /// CEC bridges); the fallback to PowerController-only keeps them
    /// discoverable.
    /// </remarks>
    [Fact]
    public void Speaker_is_only_advertised_when_VOLUME_SET_feature_is_supported()
    {
        var entity = TestEntities.MediaPlayer(
            supportedFeatures: MediaPlayerFeatures.TurnOn | MediaPlayerFeatures.TurnOff);

        var endpoint = _transformer.Transform(entity);
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldNotContain(Namespaces.Speaker);
    }

    /// <summary>
    /// The live <c>androidtv_remote</c> TV shape: step volume and the full
    /// playback set, no absolute volume.
    /// </summary>
    [Fact]
    public void Android_TV_remote_advertises_step_speaker_and_playback_but_not_speaker()
    {
        var endpoint = _transformer.Transform(
            TestEntities.MediaPlayer(supportedFeatures: TestEntities.AndroidTvRemoteFeatures, volumeLevel: null));
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldContain(Namespaces.StepSpeaker);
        interfaces.ShouldNotContain(Namespaces.Speaker);
        endpoint.Capabilities.Single(c => c.Interface == Namespaces.PlaybackController)
            .SupportedOperations.ShouldBe([DirectiveNames.Play, DirectiveNames.Pause, DirectiveNames.Stop, DirectiveNames.Next, DirectiveNames.Previous]);
        endpoint.Capabilities.Single(c => c.Interface == Namespaces.PlaybackStateReporter)
            .Properties!.Supported.Select(p => p.Name).ShouldBe([PlaybackStateReporterProperties.PlaybackState]);
    }

    /// <summary>
    /// VOLUME_SET wins over VOLUME_STEP: a player with both keeps Speaker (slider,
    /// absolute volume) and does not also get StepSpeaker.
    /// </summary>
    [Fact]
    public void Speaker_takes_precedence_over_step_speaker()
    {
        var entity = TestEntities.MediaPlayer(
            supportedFeatures: MediaPlayerFeatures.VolumeSet | MediaPlayerFeatures.VolumeStep | MediaPlayerFeatures.TurnOn);

        var interfaces = _transformer.Transform(entity).Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.Speaker);
        interfaces.ShouldNotContain(Namespaces.StepSpeaker);
    }

    /// <summary>
    /// Each playback feature bit contributes exactly its own operation.
    /// </summary>
    /// <param name="feature">The HA feature bit.</param>
    /// <param name="operation">The PlaybackController operation it maps to.</param>
    [Theory]
    [InlineData(MediaPlayerFeatures.Play, "Play")]
    [InlineData(MediaPlayerFeatures.Pause, "Pause")]
    [InlineData(MediaPlayerFeatures.Stop, "Stop")]
    [InlineData(MediaPlayerFeatures.NextTrack, "Next")]
    [InlineData(MediaPlayerFeatures.PreviousTrack, "Previous")]
    public void Playback_operations_are_gated_per_feature_bit(int feature, string operation)
    {
        var endpoint = _transformer.Transform(TestEntities.MediaPlayer(supportedFeatures: MediaPlayerFeatures.TurnOn | feature));

        endpoint.Capabilities.Single(c => c.Interface == Namespaces.PlaybackController)
            .SupportedOperations.ShouldBe([operation]);
    }

    /// <summary>
    /// No playback bits, no PlaybackController or PlaybackStateReporter — and no
    /// StepSpeaker without VOLUME_STEP.
    /// </summary>
    [Fact]
    public void Power_only_player_gets_no_playback_or_step_speaker()
    {
        var entity = TestEntities.MediaPlayer(supportedFeatures: MediaPlayerFeatures.TurnOn | MediaPlayerFeatures.TurnOff);

        var interfaces = _transformer.Transform(entity).Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldNotContain(Namespaces.PlaybackController);
        interfaces.ShouldNotContain(Namespaces.PlaybackStateReporter);
        interfaces.ShouldNotContain(Namespaces.StepSpeaker);
    }

    /// <summary>
    /// Wire shape Alexa expects: StepSpeaker and PlaybackController carry no
    /// <c>properties</c> block, and only PlaybackController has
    /// <c>supportedOperations</c>. PlaybackStateReporter does carry one:
    /// <c>playbackState</c>, proactively reported and retrievable.
    /// </summary>
    [Fact]
    public void Step_speaker_and_playback_serialize_without_properties()
    {
        var endpoint = _transformer.Transform(TestEntities.MediaPlayer(supportedFeatures: TestEntities.AndroidTvRemoteFeatures));
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        var json = JsonSerializer.SerializeToNode(endpoint.Capabilities, options)!.AsArray();
        var step = json.Single(c => (string?)c!["interface"] == Namespaces.StepSpeaker)!.AsObject();
        var playback = json.Single(c => (string?)c!["interface"] == Namespaces.PlaybackController)!.AsObject();
        var reporter = json.Single(c => (string?)c!["interface"] == Namespaces.PlaybackStateReporter)!.AsObject();

        step.ContainsKey("properties").ShouldBeFalse();
        step.ContainsKey("supportedOperations").ShouldBeFalse();
        playback.ContainsKey("properties").ShouldBeFalse();
        playback["supportedOperations"]!.AsArray().Select(o => (string?)o).ShouldBe(["Play", "Pause", "Stop", "Next", "Previous"]);
        reporter.ContainsKey("supportedOperations").ShouldBeFalse();
        var properties = reporter["properties"]!.AsObject();
        properties["supported"]!.AsArray().Select(p => (string?)p!["name"]).ShouldBe(["playbackState"]);
        ((bool?)properties["proactivelyReported"]).ShouldBe(true);
        ((bool?)properties["retrievable"]).ShouldBe(true);
    }

    /// <summary>
    /// Asserts that the <c>device_class</c> attribute drives the right Alexa
    /// display category (TV vs SPEAKER) and that the fallback for missing
    /// device_class is TV.
    /// </summary>
    /// <remarks>
    /// Most user-facing HA media players are TVs; defaulting to TV when
    /// device_class is omitted is empirically the most common correct answer.
    /// </remarks>
    /// <param name="deviceClass">The HA <c>device_class</c>.</param>
    /// <param name="expected">The expected Alexa display category.</param>
    [Theory]
    [InlineData("tv", "TV")]
    [InlineData("speaker", "SPEAKER")]
    [InlineData("receiver", "SPEAKER")]
    [InlineData(null, "TV")]
    public void Display_category_picks_per_device_class(string? deviceClass, string expected)
    {
        var endpoint = _transformer.Transform(TestEntities.MediaPlayer(deviceClass: deviceClass));
        endpoint.DisplayCategories.ShouldBe([expected]);
    }
}
