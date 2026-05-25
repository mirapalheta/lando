using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.PowerController;
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

    private static System.Text.Json.JsonElement JsonWrap(object value)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(value));
        return doc.RootElement.Clone();
    }
}
