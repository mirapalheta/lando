using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

namespace Lando.Alexa.SmartHome.Transformers.Entity.Tests;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Discovery transformers for Climate, Sensor, and Light. These pin the
/// capability-set branches that depend on device_class / supported_color_modes /
/// supported_features -- <see cref="StateTransformerTests"/> covers the matching
/// state side, and <see cref="Handlers.Directives.Tests.DiscoverDirectiveHandlerTests"/>
/// only smoke-tests that each domain produces *an* endpoint, not every branch here.
/// </summary>
public class DiscoveryTransformerTests
{
    // ---------- ClimateDiscoveryTransformer ----------

    [Fact]
    public void Climate_single_setpoint_feature_advertises_targetSetpoint_only()
    {
        var endpoint = new ClimateDiscoveryTransformer().Transform(TestEntities.Climate()); // default: TargetTemperature only

        var thermostat = endpoint.Capabilities.Single(c => c.Interface == Namespaces.ThermostatController);
        thermostat.Properties!.Supported.ShouldContain(p => p.Name == ThermostatControllerProperties.TargetSetpoint);
        thermostat.Properties.Supported.ShouldNotContain(p => p.Name == ThermostatControllerProperties.LowerSetpoint);
        thermostat.Properties.Supported.ShouldNotContain(p => p.Name == ThermostatControllerProperties.UpperSetpoint);
    }

    [Fact]
    public void Climate_dual_setpoint_feature_advertises_lower_and_upper_only()
    {
        var entity = TestEntities.Climate();
        entity.Attributes![EntityAttributes.SupportedFeatures] = JsonWrap(ClimateFeatures.TargetTemperatureRange);

        var endpoint = new ClimateDiscoveryTransformer().Transform(entity);

        var thermostat = endpoint.Capabilities.Single(c => c.Interface == Namespaces.ThermostatController);
        thermostat.Properties!.Supported.ShouldContain(p => p.Name == ThermostatControllerProperties.LowerSetpoint);
        thermostat.Properties.Supported.ShouldContain(p => p.Name == ThermostatControllerProperties.UpperSetpoint);
        thermostat.Properties.Supported.ShouldNotContain(p => p.Name == ThermostatControllerProperties.TargetSetpoint);
    }

    [Fact]
    public void Climate_with_no_hvac_modes_omits_configuration_block()
    {
        var endpoint = new ClimateDiscoveryTransformer().Transform(TestEntities.Climate()); // hvac_modes not set

        var thermostat = endpoint.Capabilities.Single(c => c.Interface == Namespaces.ThermostatController);
        thermostat.Configuration.ShouldBeNull();
    }

    [Fact]
    public void Climate_maps_and_dedupes_hvac_modes_into_configuration()
    {
        var entity = TestEntities.Climate();
        entity.Attributes![EntityAttributes.HvacModes] = JsonWrap(new[] { "heat", "cool", "heat_cool", "auto", "dry", "fan_only" });

        var endpoint = new ClimateDiscoveryTransformer().Transform(entity);

        var thermostat = endpoint.Capabilities.Single(c => c.Interface == Namespaces.ThermostatController);
        thermostat.Configuration.ShouldNotBeNull();
        var modes = thermostat.Configuration!.SupportedModes!.Cast<string>().ToArray();
        modes.ShouldContain(ThermostatModes.Heat);
        modes.ShouldContain(ThermostatModes.Cool);
        modes.ShouldContain(ThermostatModes.Auto);
        // heat_cool + auto both map to Auto and should be deduped; dry/fan_only have no
        // Alexa equivalent and are filtered out entirely.
        modes.Count(m => m == ThermostatModes.Auto).ShouldBe(1);
        modes.Length.ShouldBe(3);
    }

    [Fact]
    public void Climate_always_advertises_temperature_sensor()
    {
        var endpoint = new ClimateDiscoveryTransformer().Transform(TestEntities.Climate());

        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.TemperatureSensor);
    }

    // ---------- SensorDiscoveryTransformer ----------

    [Fact]
    public void Sensor_temperature_device_class_advertises_temperature_sensor()
    {
        var endpoint = new SensorDiscoveryTransformer().Transform(TestEntities.Sensor(deviceClass: "temperature"));

        endpoint.DisplayCategories.ShouldContain(DisplayCategory.TemperatureSensor);
        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.TemperatureSensor);
    }

    [Fact]
    public void Sensor_humidity_device_class_advertises_humidity_sensor()
    {
        var endpoint = new SensorDiscoveryTransformer().Transform(TestEntities.Sensor(deviceClass: "humidity"));

        endpoint.DisplayCategories.ShouldContain(DisplayCategory.AirQualityMonitor);
        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.HumiditySensor);
    }

    // ---------- LightDiscoveryTransformer ----------

    [Fact]
    public void Light_onoff_only_advertises_power_controller_alone()
    {
        var endpoint = new LightDiscoveryTransformer().Transform(TestEntities.Light(supportedColorModes: ["onoff"]));

        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.PowerController);
        endpoint.Capabilities.ShouldNotContain(c => c.Interface == Namespaces.BrightnessController);
        endpoint.Capabilities.ShouldNotContain(c => c.Interface == Namespaces.ColorTemperatureController);
        endpoint.Capabilities.ShouldNotContain(c => c.Interface == Namespaces.ColorController);
    }

    [Fact]
    public void Light_brightness_mode_adds_brightness_controller_only()
    {
        var endpoint = new LightDiscoveryTransformer().Transform(TestEntities.Light(supportedColorModes: ["brightness"]));

        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.BrightnessController);
        endpoint.Capabilities.ShouldNotContain(c => c.Interface == Namespaces.ColorTemperatureController);
        endpoint.Capabilities.ShouldNotContain(c => c.Interface == Namespaces.ColorController);
    }

    [Fact]
    public void Light_color_temp_mode_adds_brightness_and_color_temperature()
    {
        var endpoint = new LightDiscoveryTransformer().Transform(TestEntities.Light(supportedColorModes: ["color_temp"]));

        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.BrightnessController);
        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.ColorTemperatureController);
        endpoint.Capabilities.ShouldNotContain(c => c.Interface == Namespaces.ColorController);
    }

    [Fact]
    public void Light_chromatic_mode_adds_brightness_and_color()
    {
        var endpoint = new LightDiscoveryTransformer().Transform(TestEntities.Light(supportedColorModes: ["hs"]));

        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.BrightnessController);
        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.ColorController);
        endpoint.Capabilities.ShouldNotContain(c => c.Interface == Namespaces.ColorTemperatureController);
    }

    private static System.Text.Json.JsonElement JsonWrap(object value)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(value));
        return doc.RootElement.Clone();
    }
}
