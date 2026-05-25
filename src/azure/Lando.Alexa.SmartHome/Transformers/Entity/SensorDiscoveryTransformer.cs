using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Discovery transformer for the <c>sensor</c> HA domain. Supports
/// <c>device_class=temperature</c> (advertises <c>Alexa.TemperatureSensor</c>) and
/// <c>device_class=humidity</c> (advertises <c>Alexa.HumiditySensor</c>).
/// </summary>
/// <remarks>
/// Sensors are read-only; no control directives are registered alongside these
/// capabilities. Unknown <c>device_class</c> values fall back to
/// <c>Alexa.TemperatureSensor</c> — users who expose non-temperature/humidity
/// sensors should mark them as unexposed via the <c>lando_expose</c> attribute.
/// </remarks>
public class SensorDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity)
        => entity.GetDeviceClass() switch
        {
            SensorDeviceClasses.Humidity => DisplayCategory.AirQualityMonitor,
            SensorDeviceClasses.Temperature => DisplayCategory.TemperatureSensor,
            _ => null!
        };

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
        => entity.GetDeviceClass() switch
        {
            SensorDeviceClasses.Humidity => [Capability.HumiditySensor],
            SensorDeviceClasses.Temperature => [Capability.TemperatureSensor],
            _ => []
        };
}
