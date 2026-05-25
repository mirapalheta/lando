using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.HumiditySensor;
using Lando.Alexa.SmartHome.Models.Interfaces.TemperatureSensor;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.Alexa.SmartHome.Transformers.Entity;

namespace Lando.Alexa.SmartHome.Discovery.Tests;

/// <summary>
/// Discovery and state coverage for the <c>sensor</c> domain. Pins the
/// per-device-class capability surface so future refactors can't silently drop
/// or swap the sensor interface.
/// </summary>
public class SensorDiscoveryTests
{
    private readonly SensorDiscoveryTransformer _discovery = new();
    private readonly SensorStateTransformer _state = new();

    /// <summary>
    /// Asserts that a temperature sensor advertises only
    /// <c>Alexa.TemperatureSensor</c> and is categorised as
    /// <c>TEMPERATURE_SENSOR</c>.
    /// </summary>
    [Fact]
    public void Temperature_sensor_advertises_TemperatureSensor_capability()
    {
        var endpoint = _discovery.Transform(TestEntities.Sensor(deviceClass: "temperature"));
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.TemperatureSensor);
        interfaces.ShouldNotContain(Namespaces.HumiditySensor);
        endpoint.DisplayCategories.ShouldContain(DisplayCategory.TemperatureSensor);
    }

    /// <summary>
    /// Asserts that a humidity sensor advertises only
    /// <c>Alexa.HumiditySensor</c> and is categorised as
    /// <c>AIR_QUALITY_MONITOR</c>.
    /// </summary>
    [Fact]
    public void Humidity_sensor_advertises_HumiditySensor_capability()
    {
        var endpoint = _discovery.Transform(TestEntities.Sensor(deviceClass: "humidity"));
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldContain(Namespaces.HumiditySensor);
        interfaces.ShouldNotContain(Namespaces.TemperatureSensor);
        endpoint.DisplayCategories.ShouldContain(DisplayCategory.AirQualityMonitor);
    }

    /// <summary>
    /// Asserts that a temperature sensor state report includes a
    /// <c>TemperatureSensor.temperature</c> property with the correct value
    /// and scale.
    /// </summary>
    [Fact]
    public void Temperature_sensor_reports_temperature_with_correct_scale()
    {
        var props = _state.Transform(TestEntities.Sensor(deviceClass: "temperature", state: "72.5", unit: "°F"));

        var temp = props
            .Where(p => p.Namespace == Namespaces.TemperatureSensor && p.Name == TemperatureSensorProperties.Temperature)
            .Select(p => p.Value as Temperature)
            .Single();

        temp.ShouldNotBeNull();
        temp!.Value.ShouldBe(72.5);
        temp.Scale.ShouldBe(TemperatureScale.Fahrenheit);
    }

    /// <summary>
    /// Asserts that a Celsius temperature sensor reports the correct scale.
    /// </summary>
    [Fact]
    public void Temperature_sensor_reports_Celsius_when_unit_is_C()
    {
        var props = _state.Transform(TestEntities.Sensor(deviceClass: "temperature", state: "23.0", unit: "°C"));

        var temp = props
            .Where(p => p.Namespace == Namespaces.TemperatureSensor && p.Name == TemperatureSensorProperties.Temperature)
            .Select(p => p.Value as Temperature)
            .Single();

        temp!.Scale.ShouldBe(TemperatureScale.Celsius);
        temp.Value.ShouldBe(23.0);
    }

    /// <summary>
    /// Asserts that a humidity sensor state report includes a
    /// <c>HumiditySensor.relativeHumidity</c> property with the correct value.
    /// </summary>
    [Fact]
    public void Humidity_sensor_reports_relativeHumidity()
    {
        var props = _state.Transform(TestEntities.Sensor(deviceClass: "humidity", state: "58.3", unit: "%"));

        props.ShouldContain(p =>
            p.Namespace == Namespaces.HumiditySensor &&
            p.Name == HumiditySensorProperties.RelativeHumidity);
    }

    /// <summary>
    /// Asserts that the temperature state transformer does not emit a
    /// <c>HumiditySensor</c> property, and vice versa — the two must not bleed
    /// into each other.
    /// </summary>
    [Fact]
    public void Temperature_state_does_not_include_HumiditySensor_property()
    {
        var props = _state.Transform(TestEntities.Sensor(deviceClass: "temperature", state: "70.0"));

        props.ShouldNotContain(p => p.Namespace == Namespaces.HumiditySensor);
    }

    /// <summary>
    /// Asserts that the humidity state transformer does not emit a
    /// <c>TemperatureSensor</c> property.
    /// </summary>
    [Fact]
    public void Humidity_state_does_not_include_TemperatureSensor_property()
    {
        var props = _state.Transform(TestEntities.Sensor(deviceClass: "humidity", state: "50.0"));

        props.ShouldNotContain(p => p.Namespace == Namespaces.TemperatureSensor);
    }
}
