using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.Alexa.SmartHome.Transformers.Entity;

namespace Lando.Alexa.SmartHome.State.Tests;

/// <summary>
/// State reports must stay in lockstep with the discovery payload — every
/// retrievable capability advertised needs a reportable value, and reporting a
/// value for a capability that wasn't advertised breaks the Alexa app rendering.
/// These tests catch drift between the two surfaces.
/// </summary>
/// <remarks>
/// Each test constructs the relevant state transformer directly and asserts on
/// its output, so failures point at the specific transformer rather than at the
/// shared dispatcher.
/// </remarks>
public class StateReportTests
{
    /// <summary>
    /// Asserts that a positionable shade reports only the <c>Shade.Position</c>
    /// rangeValue — no powerState, no percentage — matching what the discovery
    /// transformer advertises.
    /// </summary>
    /// <remarks>
    /// Reporting powerState alongside rangeValue would push the Alexa app back
    /// to the horizontal-slider-with-power-pill UI; this assertion guards that
    /// regression.
    /// </remarks>
    [Fact]
    public void Shade_reports_rangeValue_only_no_powerState()
    {
        var props = new CoverStateTransformer().Transform(TestEntities.Cover(deviceClass: "shade", currentPosition: 30));

        props.ShouldContain(p => p.Namespace == Namespaces.RangeController
                              && p.Instance == Capability.ShadePositionInstance
                              && (int)p.Value! == 30);
        props.ShouldNotContain(p => p.Namespace == Namespaces.PowerController);
        props.ShouldNotContain(p => p.Namespace == Namespaces.PercentageController);
    }

    /// <summary>
    /// Asserts that a door reports only powerState.
    /// </summary>
    /// <remarks>
    /// Binary covers can't be positioned, so they must not surface a
    /// rangeValue property the user could try to set.
    /// </remarks>
    [Fact]
    public void Door_reports_powerState_only()
    {
        var props = new CoverStateTransformer().Transform(
            TestEntities.Cover(deviceClass: "door", state: "open", currentPosition: null));

        props.ShouldContain(p => p.Namespace == Namespaces.PowerController
                              && p.Value as string == PowerState.On);
        props.ShouldNotContain(p => p.Namespace == Namespaces.RangeController);
    }

    /// <summary>
    /// Asserts that a fan reports both powerState and the
    /// <c>Fan.Speed</c> rangeValue, never the legacy percentage.
    /// </summary>
    /// <remarks>
    /// Discovery dropped PercentageController on fans; the state report must
    /// follow suit so the two surfaces don't drift.
    /// </remarks>
    [Fact]
    public void Fan_reports_powerState_and_FanSpeed_rangeValue()
    {
        var props = new FanStateTransformer().Transform(TestEntities.Fan(percentage: 66));

        props.ShouldContain(p => p.Namespace == Namespaces.PowerController);
        props.ShouldContain(p => p.Namespace == Namespaces.RangeController
                              && p.Instance == Capability.FanSpeedInstance
                              && (int)p.Value! == 66);
        props.ShouldNotContain(p => p.Namespace == Namespaces.PercentageController);
    }

    /// <summary>
    /// Asserts that a media_player with VOLUME_SET reports powerState plus
    /// Speaker volume (0..100 int) and muted (bool).
    /// </summary>
    /// <remarks>
    /// Volume conversion from HA's 0..1 float to Alexa's 0..100 integer is
    /// where bugs creep in; the assertion pins the exact expected integer.
    /// </remarks>
    [Fact]
    public void MediaPlayer_reports_power_volume_and_muted()
    {
        var props = new MediaPlayerStateTransformer().Transform(
            TestEntities.MediaPlayer(volumeLevel: 0.4, isVolumeMuted: false));

        props.ShouldContain(p => p.Namespace == Namespaces.PowerController
                              && p.Value as string == PowerState.On);
        props.ShouldContain(p => p.Namespace == Namespaces.Speaker
                              && p.Name == "volume"
                              && (int)p.Value! == 40);
        props.ShouldContain(p => p.Namespace == Namespaces.Speaker
                              && p.Name == "muted"
                              && (bool)p.Value! == false);
    }

    /// <summary>
    /// Asserts that a paused media_player reports as ON — paused playback is
    /// still a powered-on TV from the user's perspective.
    /// </summary>
    /// <remarks>
    /// Treating <c>paused</c> as OFF would tell Alexa users their TV powered
    /// itself off during a pause, which is wrong and confusing.
    /// </remarks>
    [Fact]
    public void MediaPlayer_paused_state_still_reports_on()
    {
        var props = new MediaPlayerStateTransformer().Transform(TestEntities.MediaPlayer(state: "paused"));
        var power = props.Single(p => p.Namespace == Namespaces.PowerController);

        (power.Value as string).ShouldBe(PowerState.On);
    }

    /// <summary>
    /// Asserts that an off media_player reports as OFF.
    /// </summary>
    /// <remarks>
    /// Symmetric coverage with the paused test: only the literal "off" /
    /// "unavailable" / "unknown" states should report OFF.
    /// </remarks>
    [Fact]
    public void MediaPlayer_off_state_reports_off()
    {
        var props = new MediaPlayerStateTransformer().Transform(TestEntities.MediaPlayer(state: "off"));
        var power = props.Single(p => p.Namespace == Namespaces.PowerController);

        (power.Value as string).ShouldBe(PowerState.Off);
    }
}
