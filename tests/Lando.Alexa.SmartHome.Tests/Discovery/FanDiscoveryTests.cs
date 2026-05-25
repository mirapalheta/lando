using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Transformers.Entity;

namespace Lando.Alexa.SmartHome.Discovery.Tests;


/// <summary>
/// Fan discovery always emits PowerController. Fans that advertise the
/// <c>SET_SPEED</c> bit additionally get <see cref="Capability.FanSpeed"/> with
/// low/medium/high presets and Raise/Lower semantics; fans without
/// <c>SET_SPEED</c> stay on PowerController alone.
/// </summary>
/// <remarks>
/// The transformer replaces the legacy PercentageController-shaped fan with the
/// modern RangeController shape, so these tests also pin that
/// PercentageController is never advertised on a fan today.
/// </remarks>
public class FanDiscoveryTests
{
    private readonly FanDiscoveryTransformer _transformer = new();

    /// <summary>
    /// Asserts that a fan with <c>SET_SPEED</c> advertises PowerController plus
    /// the <see cref="Capability.FanSpeed"/> RangeController, with presets
    /// populated.
    /// </summary>
    /// <remarks>
    /// Presence of presets is what gives users "set the fan to medium" voice
    /// support, so the assertion checks the presets are populated rather than
    /// taking the RangeController instance as sufficient.
    /// </remarks>
    [Fact]
    public void Fan_with_SetSpeed_advertises_PowerController_and_FanSpeed_range()
    {
        var endpoint = _transformer.Transform(TestEntities.Fan());
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldContain(Namespaces.RangeController);
        interfaces.ShouldNotContain(Namespaces.PercentageController);

        var range = endpoint.Capabilities.Single(c => c.Interface == Namespaces.RangeController);
        range.Instance.ShouldBe(Capability.FanSpeedInstance);
        range.Configuration!.Presets.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Asserts that a fan without <c>SET_SPEED</c> still surfaces as a
    /// controllable Alexa endpoint, exposing only PowerController.
    /// </summary>
    /// <remarks>
    /// Some HA fans only expose on/off — advertising RangeController without
    /// SET_SPEED would give users a non-functional slider.
    /// </remarks>
    [Fact]
    public void Fan_without_SetSpeed_only_advertises_PowerController()
    {
        var endpoint = _transformer.Transform(TestEntities.Fan(supportedFeatures: 0));
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldNotContain(Namespaces.RangeController);
    }

    /// <summary>
    /// Asserts that the static <see cref="Capability.FanSpeed"/> definition
    /// includes presets for the three common speeds users speak.
    /// </summary>
    /// <remarks>
    /// Low, Medium, and High match Amazon's canonical
    /// <c>Alexa.Value.Low/Medium/High</c> assets — voice resolution depends on
    /// the asset id, not the literal string.
    /// </remarks>
    [Fact]
    public void FanSpeed_presets_cover_low_medium_high()
    {
        var presetAssets = Capability.FanSpeed.Configuration!.Presets!
            .SelectMany(p => p.PresetResources!.FriendlyNames)
            .Select(fn => fn.Value.AssetId)
            .ToList();

        presetAssets.ShouldContain("Alexa.Value.Low");
        presetAssets.ShouldContain("Alexa.Value.Medium");
        presetAssets.ShouldContain("Alexa.Value.High");
    }
}
