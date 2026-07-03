using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorController;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

namespace Lando.Alexa.SmartHome.Transformers.Entity.Tests;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// State transformers for Light, Switch, and Climate. Each must emit exactly
/// the properties their matching discovery transformer advertised — drift
/// surfaces in the Alexa app as "device unresponsive" because retrievable
/// properties without corresponding values are treated as endpoint health
/// failures.
/// </summary>
public class StateTransformerTests
{
    // ---------- LightStateTransformer ----------

    [Theory]
    [InlineData("on", PowerState.On)]
    [InlineData("off", PowerState.Off)]
    public void Light_reports_powerState(string state, string expected)
    {
        var props = new LightStateTransformer().Transform(TestEntities.Light(state: state));
        var power = props.Single(p => p.Namespace == Namespaces.PowerController);
        power.Name.ShouldBe(PowerControllerProperties.PowerState);
        power.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(128, 50)]
    [InlineData(255, 100)]
    public void Light_with_brightness_scales_HA_0_to_255_into_Alexa_0_to_100(int brightness255, int expectedPct)
    {
        var props = new LightStateTransformer().Transform(TestEntities.Light(brightness255: brightness255));
        props.Single(p => p.Namespace == Namespaces.BrightnessController).Value.ShouldBe(expectedPct);
    }

    [Fact]
    public void Light_without_brightness_omits_BrightnessController()
    {
        var props = new LightStateTransformer().Transform(TestEntities.Light(brightness255: null));
        props.Any(p => p.Namespace == Namespaces.BrightnessController).ShouldBeFalse();
    }

    [Theory]
    [InlineData(500, 2000)]   // 500 mired → 2000K
    [InlineData(250, 4000)]   // 250 mired → 4000K
    public void Light_converts_mired_to_kelvin(int mired, int expectedKelvin)
    {
        var props = new LightStateTransformer().Transform(TestEntities.Light(colorTempMired: mired));
        props.Single(p => p.Namespace == Namespaces.ColorTemperatureController).Value.ShouldBe(expectedKelvin);
    }

    [Fact]
    public void Light_without_color_temp_omits_ColorTemperatureController()
    {
        var props = new LightStateTransformer().Transform(TestEntities.Light(colorTempMired: null));
        props.Any(p => p.Namespace == Namespaces.ColorTemperatureController).ShouldBeFalse();
    }

    [Fact]
    public void Light_with_hs_color_and_brightness_emits_ColorController()
    {
        var entity = TestEntities.Light(brightness255: 128, hs_color: ["300", "50"]);

        var props = new LightStateTransformer().Transform(entity);

        var color = props.Single(p => p.Namespace == Namespaces.ColorController);
        var hsb = (HsbColor)color.Value!;
        hsb.Hue.ShouldBe(300);
        hsb.Saturation.ShouldBe(0.5);
        hsb.Brightness.ShouldBe(128 / 255.0);
    }

    [Fact]
    public void Light_with_hs_color_and_no_brightness_defaults_ColorController_brightness_to_full()
    {
        var entity = TestEntities.Light(brightness255: null, hs_color: ["120", "80"]);

        var props = new LightStateTransformer().Transform(entity);

        var color = props.Single(p => p.Namespace == Namespaces.ColorController);
        ((HsbColor)color.Value!).Brightness.ShouldBe(1.0);
    }

    [Fact]
    public void Light_without_hs_color_omits_ColorController()
    {
        var props = new LightStateTransformer().Transform(TestEntities.Light());
        props.Any(p => p.Namespace == Namespaces.ColorController).ShouldBeFalse();
    }

    [Fact]
    public void Light_with_incomplete_hs_color_omits_ColorController()
    {
        var entity = TestEntities.Light(hs_color: ["300"]); // only one element

        var props = new LightStateTransformer().Transform(entity);

        props.Any(p => p.Namespace == Namespaces.ColorController).ShouldBeFalse();
    }

    // ---------- SwitchStateTransformer ----------

    [Theory]
    [InlineData("on", PowerState.On)]
    [InlineData("off", PowerState.Off)]
    public void Switch_reports_only_powerState(string state, string expected)
    {
        var props = new SwitchStateTransformer().Transform(TestEntities.Switch(state: state)).ToArray();
        props.Length.ShouldBe(2);
        props[0].Namespace.ShouldBe(Namespaces.EndpointHealth);
        props[1].Namespace.ShouldBe(Namespaces.PowerController);
        props[1].Value.ShouldBe(expected);
    }

    // ---------- ClimateStateTransformer ----------

    [Fact]
    public void Climate_single_setpoint_reports_temperature_targetSetpoint_and_mode()
    {
        var entity = TestEntities.Climate(state: "heat", currentTemp: 70, targetTemp: 72);
        var props = new ClimateStateTransformer().Transform(entity).ToArray();

        props.ShouldContain(p =>
            p.Namespace == Namespaces.TemperatureSensor && p.Name == "temperature");
        props.ShouldContain(p =>
            p.Namespace == Namespaces.ThermostatController && p.Name == ThermostatControllerProperties.TargetSetpoint);
        props.ShouldContain(p =>
            p.Namespace == Namespaces.ThermostatController
            && p.Name == ThermostatControllerProperties.ThermostatMode
            && (string)p.Value! == ThermostatModes.Heat);
        // single-setpoint mode → no Lower/Upper
        props.Any(p => p.Name == ThermostatControllerProperties.LowerSetpoint).ShouldBeFalse();
        props.Any(p => p.Name == ThermostatControllerProperties.UpperSetpoint).ShouldBeFalse();
    }

    [Theory]
    [InlineData("heat", ThermostatModes.Heat)]
    [InlineData("cool", ThermostatModes.Cool)]
    [InlineData("heat_cool", ThermostatModes.Auto)]
    [InlineData("auto", ThermostatModes.Auto)]
    [InlineData("off", ThermostatModes.Off)]
    [InlineData("fan_only", ThermostatModes.Auto)] // unmapped → Auto fallback
    public void Climate_maps_hvac_state_to_alexa_mode(string haState, string expected)
    {
        var entity = TestEntities.Climate(state: haState);
        var props = new ClimateStateTransformer().Transform(entity);
        var mode = props.Single(p => p.Name == ThermostatControllerProperties.ThermostatMode);
        ((string)mode.Value!).ShouldBe(expected);
    }

    [Theory]
    [InlineData("°F", TemperatureScale.Fahrenheit)]
    [InlineData("°C", TemperatureScale.Celsius)]
    public void Climate_picks_temperature_scale_from_unit(string unit, string expectedScale)
    {
        var entity = TestEntities.Climate(unit: unit, currentTemp: 21);
        var props = new ClimateStateTransformer().Transform(entity);
        var temp = props.Single(p => p.Namespace == Namespaces.TemperatureSensor);
        ((Temperature)temp.Value!).Scale.ShouldBe(expectedScale);
    }

    [Fact]
    public void Climate_dual_setpoint_emits_lower_and_upper()
    {
        // TARGET_TEMPERATURE_RANGE supported_features bit + explicit low/high attrs
        var entity = TestEntities.Climate();
        entity.Attributes ??= [];
        entity.Attributes[EntityAttributes.SupportedFeatures] = JsonWrap(ClimateFeatures.TargetTemperatureRange);
        entity.Attributes[EntityAttributes.TargetTempLow] = JsonWrap(68.0);
        entity.Attributes[EntityAttributes.TargetTempHigh] = JsonWrap(75.0);

        var props = new ClimateStateTransformer().Transform(entity).ToArray();

        var lower = props.Single(p => p.Name == ThermostatControllerProperties.LowerSetpoint);
        var upper = props.Single(p => p.Name == ThermostatControllerProperties.UpperSetpoint);
        ((Temperature)lower.Value!).Value.ShouldBe(68);
        ((Temperature)upper.Value!).Value.ShouldBe(75);
        // dual mode → no single TargetSetpoint
        props.Any(p => p.Name == ThermostatControllerProperties.TargetSetpoint).ShouldBeFalse();
    }

    [Fact]
    public void Climate_with_no_temperatures_still_emits_neutral_temperature_zero()
    {
        var entity = TestEntities.Climate(currentTemp: null, targetTemp: null);
        var props = new ClimateStateTransformer().Transform(entity).ToArray();

        var temp = props.Single(p => p.Namespace == Namespaces.TemperatureSensor);
        ((Temperature)temp.Value!).Value.ShouldBe(0);
    }

    // ---------- CoverStateTransformer ----------

    [Fact]
    public void Cover_shade_with_set_position_reports_range_value_from_current_position()
    {
        var entity = TestEntities.Cover(deviceClass: "shade", currentPosition: 42);
        var props = new CoverStateTransformer().Transform(entity).ToArray();

        var range = props.Single(p => p.Namespace == Namespaces.RangeController);
        range.Instance.ShouldBe(Capability.ShadePositionInstance);
        range.Name.ShouldBe(RangeControllerProperties.RangeValue);
        range.Value.ShouldBe(42);
        props.Any(p => p.Namespace == Namespaces.PowerController).ShouldBeFalse();
    }

    [Fact]
    public void Cover_shade_with_set_position_but_no_current_position_defaults_to_zero()
    {
        var entity = TestEntities.Cover(deviceClass: "shade", currentPosition: null);
        var props = new CoverStateTransformer().Transform(entity);

        props.Single(p => p.Namespace == Namespaces.RangeController).Value.ShouldBe(0);
    }

    [Theory]
    [InlineData("open", PowerState.On)]
    [InlineData("opening", PowerState.On)]
    [InlineData("on", PowerState.On)]
    [InlineData("closed", PowerState.Off)]
    [InlineData("closing", PowerState.Off)]
    public void Cover_binary_device_reports_power_state(string state, string expected)
    {
        // "garage" is not shade-like, so this always takes the PowerController
        // branch regardless of the SetPosition feature bit.
        var entity = TestEntities.Cover(deviceClass: "garage", state: state);
        var props = new CoverStateTransformer().Transform(entity).ToArray();

        var power = props.Single(p => p.Namespace == Namespaces.PowerController);
        power.Value.ShouldBe(expected);
        props.Any(p => p.Namespace == Namespaces.RangeController).ShouldBeFalse();
    }

    [Fact]
    public void Cover_shade_without_set_position_feature_falls_back_to_power_state()
    {
        var entity = TestEntities.Cover(deviceClass: "shade", supportedFeatures: CoverFeatures.Open | CoverFeatures.Close, state: "open");
        var props = new CoverStateTransformer().Transform(entity);

        props.Any(p => p.Namespace == Namespaces.RangeController).ShouldBeFalse();
        props.Any(p => p.Namespace == Namespaces.PowerController).ShouldBeTrue();
    }

    private static System.Text.Json.JsonElement JsonWrap(object value)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(value));
        return doc.RootElement.Clone();
    }
}
