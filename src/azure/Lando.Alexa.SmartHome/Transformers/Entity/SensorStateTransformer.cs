using System;
using System.Collections.Generic;
using System.Globalization;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.HumiditySensor;
using Lando.Alexa.SmartHome.Models.Interfaces.TemperatureSensor;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// State transformer for the <c>sensor</c> HA domain. Reports
/// <c>Alexa.TemperatureSensor.temperature</c> for temperature sensors and
/// <c>Alexa.HumiditySensor.relativeHumidity</c> for humidity sensors.
/// </summary>
/// <remarks>
/// The sensor reading lives in <see cref="HomeAssistantEntity.State"/> as a
/// numeric string (e.g. <c>"23.5"</c>), not in the attributes bag. Unparseable
/// states fall back to <c>0</c> — this only occurs when HA marks the entity
/// <c>unavailable</c> or <c>unknown</c>, in which case the
/// <c>Alexa.EndpointHealth</c> property will already signal <c>UNREACHABLE</c>.
/// </remarks>
public class SensorStateTransformer : StateTransformerBase
{
    /// <inheritdoc />
    protected override IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity)
    {
        var reading = double.TryParse(entity.State, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0d;

        return entity.GetDeviceClass() switch
        {
            SensorDeviceClasses.Humidity =>
            [
                new()
                {
                    Namespace = Namespaces.HumiditySensor,
                    Name = HumiditySensorProperties.RelativeHumidity,
                    Value = new {
                        Value = reading,
                    },
                    TimeOfSample = entity.LastUpdated,
                    UncertaintyInMilliseconds = DefaultUncertaintyMs
                }
            ],
            SensorDeviceClasses.Temperature =>
            [
                new()
                {
                    Namespace = Namespaces.TemperatureSensor,
                    Name = TemperatureSensorProperties.Temperature,
                    Value = new Temperature {
                        Value = reading,
                        Scale = ResolveTemperatureScale(entity.GetUnitOfMeasurement())
                    },
                    TimeOfSample = entity.LastUpdated,
                    UncertaintyInMilliseconds = DefaultUncertaintyMs
                }
            ],
            _ => []
        };
    }

    /// <summary>
    /// Picks the Alexa temperature scale from the HA <c>unit_of_measurement</c>
    /// string, defaulting to Fahrenheit when the entity omits a unit.
    /// </summary>
    private static string ResolveTemperatureScale(string? entityUnit, string fallbackUnit = "°F")
        => (entityUnit ?? fallbackUnit).Contains('F', StringComparison.OrdinalIgnoreCase)
            ? TemperatureScale.Fahrenheit
            : TemperatureScale.Celsius;
}
