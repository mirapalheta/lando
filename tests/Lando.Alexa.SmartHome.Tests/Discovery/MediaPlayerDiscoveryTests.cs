using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Transformers.Entity;

namespace Lando.Alexa.SmartHome.Discovery.Tests;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Media player discovery emits PowerController unconditionally and layers on
/// Speaker when the entity advertises the <c>VOLUME_SET</c> feature. Playback,
/// Channel, and Input controllers are intentionally not advertised yet.
/// </summary>
/// <remarks>
/// HA media_player integrations vary widely in how they implement playback and
/// input switching; advertising those controllers would create silent partial
/// failures on integrations that can't honour them. Power + volume is the
/// reliable universal subset for TVs and speakers.
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
