using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Discovery transformer for the <c>sensor</c> HA domain. Supports
/// <c>device_class=temperature</c> (advertises <c>Alexa.TemperatureSensor</c>) and
/// <c>device_class=humidity</c> (advertises only the read-only
/// <see cref="Capability.HumidityRange"/> RangeController, which is what the
/// <c>AIR_QUALITY_MONITOR</c> app tile and voice read).
/// </summary>
/// <remarks>
/// Sensors are read-only; no control directives are registered alongside these
/// capabilities. Humidity sensors deliberately do not advertise
/// <c>Alexa.HumiditySensor</c>: the app draws an extra "Humidity --" row for it
/// on the air-quality-monitor tile and never fills it, while the value shown
/// (and the voice answer) come from the RangeController. Unknown
/// <c>device_class</c> values fall back to <c>Alexa.TemperatureSensor</c> —
/// users who expose non-temperature/humidity sensors should mark them as
/// unexposed via the <c>lando_expose</c> attribute.
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
            SensorDeviceClasses.Humidity => [Capability.HumidityRange],
            SensorDeviceClasses.Temperature => [Capability.TemperatureSensor],
            _ => []
        };
}
