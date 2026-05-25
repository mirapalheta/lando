using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Transformers.Entity;

namespace Lando.Alexa.SmartHome.Discovery.Tests;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Cover discovery is bimodal: positionable shade-likes get
/// <see cref="Capability.ShadePosition"/>; binary covers get
/// <see cref="Capability.PowerController"/>. Drift in either direction breaks the
/// Alexa app rendering, so the assertions below pin both branches.
/// </summary>
/// <remarks>
/// These tests construct the transformer directly rather than going through DI,
/// keeping the surface small and the failure messages tight when a regression
/// lands. Display category branching also lives in
/// <see cref="CoverDiscoveryTransformer"/>, so it is asserted on here too.
/// </remarks>
public class CoverDiscoveryTests
{
    private readonly CoverDiscoveryTransformer _transformer = new();

    /// <summary>
    /// Asserts that every shade-like device class produces only the
    /// <see cref="Capability.ShadePosition"/> RangeController capability, and not
    /// the light-style PowerController or PercentageController.
    /// </summary>
    /// <remarks>
    /// Includes the explicit shade-like classes plus <c>null</c> — HA users
    /// frequently omit <c>device_class</c> on blind integrations, and the
    /// fallback must still render correctly.
    /// </remarks>
    /// <param name="deviceClass">The HA <c>device_class</c> to exercise.</param>
    [Theory]
    [InlineData("shade")]
    [InlineData("blind")]
    [InlineData("shutter")]
    [InlineData("curtain")]
    [InlineData("awning")]
    [InlineData("window")]
    [InlineData(null)]
    public void Positionable_shade_advertises_only_ShadePosition_range_controller(string? deviceClass)
    {
        var entity = TestEntities.Cover(deviceClass: deviceClass);

        var endpoint = _transformer.Transform(entity);
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.RangeController);
        interfaces.ShouldNotContain(Namespaces.PowerController);
        interfaces.ShouldNotContain(Namespaces.PercentageController);

        var range = endpoint.Capabilities.Single(c => c.Interface == Namespaces.RangeController);
        range.Instance.ShouldBe(Capability.ShadePositionInstance);
        // Shouldly has no ShouldContainSingle(predicate) — filter first, then assert single.
        range.CapabilityResources!.FriendlyNames
            .Where(fn => fn.Value.AssetId == "Alexa.Setting.Opening")
            .ShouldHaveSingleItem();
    }

    /// <summary>
    /// Asserts that doors, garage doors, and gates expose only
    /// <see cref="Capability.PowerController"/>.
    /// </summary>
    /// <remarks>
    /// Binary covers don't have a position — advertising RangeController would
    /// give the user a slider that can't be honoured, which is worse than just
    /// showing the open/close pill.
    /// </remarks>
    /// <param name="deviceClass">The HA cover device class.</param>
    [Theory]
    [InlineData("door")]
    [InlineData("garage")]
    [InlineData("gate")]
    public void Binary_cover_advertises_only_PowerController(string deviceClass)
    {
        var endpoint = _transformer.Transform(TestEntities.Cover(deviceClass: deviceClass));
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldNotContain(Namespaces.RangeController);
    }

    /// <summary>
    /// Asserts that a shade-like cover lacking the <c>SET_POSITION</c> feature
    /// degrades gracefully to <see cref="Capability.PowerController"/>.
    /// </summary>
    /// <remarks>
    /// Some HA blind integrations expose only open/close — without
    /// SET_POSITION the bridge can't honour a slider drag, so the fallback
    /// gives users a working binary UI instead of a non-functional slider.
    /// </remarks>
    [Fact]
    public void Shade_without_SetPosition_falls_back_to_PowerController()
    {
        var entity = TestEntities.Cover(
            deviceClass: "shade",
            supportedFeatures: CoverFeatures.Open | CoverFeatures.Close);

        var endpoint = _transformer.Transform(entity);
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldNotContain(Namespaces.RangeController);
    }

    /// <summary>
    /// Asserts that every supported HA cover <c>device_class</c> maps to the
    /// expected Alexa display category.
    /// </summary>
    /// <remarks>
    /// Display category drives both the icon Alexa shows and which voice
    /// utterances match; getting it wrong (for example a "garage door" rendering
    /// as a blind) confuses Alexa's intent resolver.
    /// </remarks>
    /// <param name="deviceClass">The HA cover device class.</param>
    /// <param name="expectedCategory">The expected Alexa display category.</param>
    [Theory]
    [InlineData("shade", "INTERIOR_BLIND")]
    [InlineData("blind", "INTERIOR_BLIND")]
    [InlineData("curtain", "CURTAIN")]
    [InlineData("awning", "AWNING")]
    [InlineData("door", "DOOR")]
    [InlineData("garage", "GARAGE_DOOR")]
    [InlineData("gate", "GARAGE_DOOR")]
    public void Display_category_picks_per_device_class(string deviceClass, string expectedCategory)
    {
        var endpoint = _transformer.Transform(TestEntities.Cover(deviceClass: deviceClass));
        endpoint.DisplayCategories.ShouldBe([expectedCategory]);
    }
}
