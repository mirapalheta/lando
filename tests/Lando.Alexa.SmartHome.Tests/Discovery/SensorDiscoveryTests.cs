using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;
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

    // Naming and null handling as Lando.FunctionApp's Program.cs configures them for responses.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
    /// Asserts that a humidity sensor advertises the <c>Sensor.Humidity</c>
    /// RangeController as its only humidity capability (no
    /// <c>Alexa.HumiditySensor</c>, no temperature) and is categorised as
    /// <c>AIR_QUALITY_MONITOR</c>.
    /// </summary>
    /// <remarks>
    /// <c>Alexa.HumiditySensor</c> is left out on purpose: on this display
    /// category the Alexa app renders an empty "Humidity --" row for it.
    /// </remarks>
    [Fact]
    public void Humidity_sensor_advertises_only_the_humidity_RangeController()
    {
        var endpoint = _discovery.Transform(TestEntities.Sensor(deviceClass: "humidity"));
        var interfaces = endpoint.Capabilities.Select(c => c.Interface).ToList();

        interfaces.ShouldNotContain(Namespaces.HumiditySensor);
        interfaces.ShouldNotContain(Namespaces.TemperatureSensor);
        endpoint.Capabilities.Count(c => c.Interface == Namespaces.RangeController).ShouldBe(1);
        endpoint.Capabilities.ShouldContain(c => c.Interface == Namespaces.RangeController
                                              && c.Instance == Capability.HumidityInstance);
        endpoint.DisplayCategories.ShouldContain(DisplayCategory.AirQualityMonitor);
    }

    /// <summary>
    /// Wire shape of the humidity RangeController Alexa's air quality monitor
    /// template expects: instance <c>Sensor.Humidity</c>, asset + text friendly
    /// names, 0..100 step 1 in percent, and a read-only <c>rangeValue</c>.
    /// </summary>
    /// <remarks>
    /// No semantics or presets: the instance is non-controllable, so nothing
    /// should invite a <c>SetRangeValue</c>.
    /// </remarks>
    [Fact]
    public void Humidity_range_serializes_as_noncontrollable_percent_range()
    {
        var endpoint = _discovery.Transform(TestEntities.Sensor(deviceClass: "humidity"));

        var json = JsonSerializer.SerializeToNode(endpoint.Capabilities, SerializerOptions)!.AsArray();
        var range = json.Single(c => (string?)c!["interface"] == Namespaces.RangeController)!.AsObject();

        ((string?)range["type"]).ShouldBe("AlexaInterface");
        ((string?)range["version"]).ShouldBe("3");
        ((string?)range["instance"]).ShouldBe("Sensor.Humidity");

        var properties = range["properties"]!.AsObject();
        properties["supported"]!.AsArray().Select(p => (string?)p!["name"]).ShouldBe(["rangeValue"]);
        ((bool?)properties["proactivelyReported"]).ShouldBe(true);
        ((bool?)properties["retrievable"]).ShouldBe(true);
        ((bool?)properties["nonControllable"]).ShouldBe(true);

        var names = range["capabilityResources"]!["friendlyNames"]!.AsArray();
        names.Count.ShouldBe(2);
        ((string?)names[0]!["@type"]).ShouldBe("asset");
        ((string?)names[0]!["value"]!["assetId"]).ShouldBe("Alexa.AirQuality.Humidity");
        ((string?)names[1]!["@type"]).ShouldBe("text");
        ((string?)names[1]!["value"]!["text"]).ShouldBe("Humidity");
        ((string?)names[1]!["value"]!["locale"]).ShouldBe("en-US");

        var configuration = range["configuration"]!.AsObject();
        var supportedRange = configuration["supportedRange"]!.AsObject();
        ((double?)supportedRange["minimumValue"]).ShouldBe(0);
        ((double?)supportedRange["maximumValue"]).ShouldBe(100);
        ((double?)supportedRange["precision"]).ShouldBe(1);
        ((string?)configuration["unitOfMeasure"]).ShouldBe("Alexa.Unit.Percent");
        configuration.ContainsKey("presets").ShouldBeFalse();

        range.ContainsKey("semantics").ShouldBeFalse();
    }

    /// <summary>
    /// Asserts that temperature sensors are untouched by the humidity range:
    /// <c>Alexa.TemperatureSensor</c> only, no RangeController.
    /// </summary>
    [Fact]
    public void Temperature_sensor_does_not_advertise_RangeController()
    {
        var endpoint = _discovery.Transform(TestEntities.Sensor(deviceClass: "temperature"));

        endpoint.Capabilities.ShouldNotContain(c => c.Interface == Namespaces.RangeController);
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
    /// Asserts that a humidity sensor state report carries no
    /// <c>HumiditySensor.relativeHumidity</c> property: the capability is not
    /// advertised, so reporting it would be a property of an undeclared
    /// interface.
    /// </summary>
    [Fact]
    public void Humidity_sensor_does_not_report_relativeHumidity()
    {
        var props = _state.Transform(TestEntities.Sensor(deviceClass: "humidity", state: "58.3", unit: "%"));

        props.ShouldNotContain(p => p.Namespace == Namespaces.HumiditySensor);
    }

    /// <summary>
    /// Wire shape of a humidity StateReport: EndpointHealth plus exactly one
    /// domain property, the <c>Sensor.Humidity</c> <c>rangeValue</c> with the
    /// reading rounded to a whole percent, stamped with the entity's sample time.
    /// </summary>
    [Fact]
    public void Humidity_state_report_carries_only_rangeValue()
    {
        var entity = TestEntities.Sensor(deviceClass: "humidity", state: "58.5", unit: "%");
        var props = _state.Transform(entity);

        var json = JsonSerializer.SerializeToNode(props, SerializerOptions)!.AsArray();
        json.Count.ShouldBe(2);
        json.ShouldContain(p => (string?)p!["namespace"] == Namespaces.EndpointHealth);
        var range = json.Single(p => (string?)p!["namespace"] == Namespaces.RangeController)!.AsObject();

        ((string?)range["instance"]).ShouldBe(Capability.HumidityInstance);
        ((string?)range["name"]).ShouldBe(RangeControllerProperties.RangeValue);
        ((int?)range["value"]).ShouldBe(59);
        ((string?)range["timeOfSample"]).ShouldBe(JsonSerializer.SerializeToNode(entity.LastUpdated, SerializerOptions)!.GetValue<string>());
    }

    /// <summary>
    /// Asserts that a temperature StateReport is unchanged: one
    /// <c>temperature</c> property, no RangeController.
    /// </summary>
    [Fact]
    public void Temperature_state_report_has_no_rangeValue()
    {
        var props = _state.Transform(TestEntities.Sensor(deviceClass: "temperature", state: "70.0")).ToList();

        props.ShouldNotContain(p => p.Namespace == Namespaces.RangeController);
        props.Count(p => p.Namespace == Namespaces.TemperatureSensor).ShouldBe(1);
    }

    /// <summary>
    /// Asserts that the temperature state transformer does not emit a
    /// <c>HumiditySensor</c> property, and that the humidity side does not emit
    /// a <c>TemperatureSensor</c> property — the two must not bleed into each
    /// other.
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
