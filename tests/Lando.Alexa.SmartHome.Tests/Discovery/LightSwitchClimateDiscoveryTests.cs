using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Transformers.Entity;

namespace Lando.Alexa.SmartHome.Discovery.Tests;

/// <summary>
/// Regression coverage for the domains whose discovery hasn't changed materially
/// in this refactor: lights, switches, and climate. Each test pins the
/// per-domain capability surface so future refactors can't quietly drop a
/// controller.
/// </summary>
/// <remarks>
/// These tests construct the transformers directly so they fail with precise
/// messages — going through DI would only obscure regressions.
/// </remarks>
public class LightSwitchClimateDiscoveryTests
{
    private readonly LightDiscoveryTransformer _light = new();
    private readonly SwitchDiscoveryTransformer _switch = new();
    private readonly ClimateDiscoveryTransformer _climate = new();

    /// <summary>
    /// Asserts that an on/off-only light advertises only PowerController.
    /// </summary>
    /// <remarks>
    /// Without a brightness or color attribute on supported_color_modes, the
    /// transformer must not attach Brightness or Color controllers — doing so
    /// would surface a slider the bulb can't honour.
    /// </remarks>
    [Fact]
    public void Onoff_light_only_advertises_PowerController()
    {
        var endpoint = _light.Transform(TestEntities.Light(supportedColorModes: ["onoff"]));
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldNotContain(Namespaces.BrightnessController);
        interfaces.ShouldNotContain(Namespaces.ColorController);
    }

    /// <summary>
    /// Asserts that a dimmable light gains BrightnessController on top of
    /// PowerController.
    /// </summary>
    /// <remarks>
    /// Any color mode other than <c>onoff</c> implies brightness support per
    /// modern HA — the assertion exercises this rule with the simplest mode
    /// ("brightness") so failures point straight at the rule.
    /// </remarks>
    [Fact]
    public void Dimmable_light_advertises_brightness()
    {
        var endpoint = _light.Transform(TestEntities.Light(supportedColorModes: ["brightness"]));
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldContain(Namespaces.BrightnessController);
    }

    /// <summary>
    /// Asserts that a colour-aware light advertises Color and Brightness
    /// controllers in addition to PowerController.
    /// </summary>
    /// <remarks>
    /// Any chromatic mode (hs/rgb/rgbw/rgbww/xy) implies both colour and
    /// brightness; tests use <c>hs</c> as the canonical chromatic mode.
    /// </remarks>
    [Fact]
    public void Color_light_advertises_color_and_brightness()
    {
        var endpoint = _light.Transform(TestEntities.Light(supportedColorModes: ["hs"]));
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldContain(Namespaces.BrightnessController);
        interfaces.ShouldContain(Namespaces.ColorController);
    }

    /// <summary>
    /// Asserts that a switch advertises only PowerController.
    /// </summary>
    /// <remarks>
    /// Switches are binary devices and should never carry a slider or any
    /// non-power capability.
    /// </remarks>
    [Fact]
    public void Switch_advertises_PowerController_only()
    {
        var endpoint = _switch.Transform(TestEntities.Switch());
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.PowerController);
        interfaces.ShouldNotContain(Namespaces.BrightnessController);
        interfaces.ShouldNotContain(Namespaces.RangeController);
    }

    /// <summary>
    /// Asserts that a climate entity advertises both ThermostatController and
    /// TemperatureSensor.
    /// </summary>
    /// <remarks>
    /// Reporting current temperature alongside the setpoint is what gives users
    /// "what's the temperature in the living room" — dropping
    /// TemperatureSensor would break that without affecting set commands.
    /// </remarks>
    [Fact]
    public void Climate_advertises_thermostat_and_temperature_sensor()
    {
        var endpoint = _climate.Transform(TestEntities.Climate());
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.ThermostatController);
        interfaces.ShouldContain(Namespaces.TemperatureSensor);
    }
}
