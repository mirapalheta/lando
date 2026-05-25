using System.Collections.Generic;
using System.Linq;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Discovery;
using Lando.Alexa.SmartHome.Models.Interfaces.TemperatureSensor;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// Discovery transformer for the <c>climate</c> HA domain. Advertises
/// <c>Alexa.ThermostatController</c> and <c>Alexa.TemperatureSensor</c> in
/// a shape that mirrors what the state transformer can reliably report.
/// </summary>
/// <remarks>
/// Picks <em>either</em> single-setpoint (<c>targetSetpoint</c>) <em>or</em>
/// dual-setpoint (<c>lowerSetpoint</c> + <c>upperSetpoint</c>) based on the
/// entity's <c>supported_features</c> bits. Advertising both at once would
/// force the state transformer to populate three setpoints on every report,
/// which HA doesn't expose simultaneously and which makes Alexa flag the
/// endpoint unhealthy.
/// <para>
/// Skips the optional <c>configuration.supportedModes</c> block when HA
/// hasn't declared any HVAC modes — emitting an empty array there causes
/// the Alexa app to render the thermostat with a "Custom" mode picker
/// instead of using its built-in defaults.
/// </para>
/// </remarks>
public class ClimateDiscoveryTransformer : DiscoveryTransformerBase
{
    /// <inheritdoc />
    protected override string GetDisplayCategory(HomeAssistantEntity entity) => DisplayCategory.Thermostat;

    /// <inheritdoc />
    protected override IEnumerable<Capability> GetDomainCapabilities(HomeAssistantEntity entity)
    {
        var features = entity.GetSupportedFeatures();
        var supportedModes = BuildThermostatModes(entity.GetHvacModes());

        yield return new Capability
        {
            Interface = Namespaces.ThermostatController,
            Properties = new CapabilityProperties
            {
                Supported = [.. BuildThermostatPropertyList(features)]
            },
            Configuration = supportedModes.Length > 0 ? new CapabilityConfiguration
            {
                SupportedModes = [.. supportedModes.Cast<object>()]
            } : null
        };

        yield return new Capability
        {
            Interface = Namespaces.TemperatureSensor,
            Properties = new CapabilityProperties
            {
                Supported = [new(TemperatureSensorProperties.Temperature)]
            }
        };
    }

    /// <summary>
    /// Maps HA HVAC mode strings to their Alexa ThermostatController
    /// equivalents, filtering out modes with no Alexa representation (dry,
    /// fan_only) and deduplicating overlapping mappings (HA's
    /// <c>heat_cool</c> and <c>auto</c> both map to <c>AUTO</c>).
    /// </summary>
    /// <remarks>
    /// Returns an empty sequence when HA exposes no <c>hvac_modes</c> —
    /// callers must skip the configuration block in that case rather than
    /// emit an empty array.
    /// </remarks>
    /// <param name="modes">The HA <c>hvac_modes</c> attribute values.</param>
    /// <returns>Deduplicated Alexa thermostat mode strings.</returns>
    private static string[] BuildThermostatModes(IReadOnlyList<string> modes)
        => [.. modes.Select(s => s switch
            {
                HvacModes.Off => ThermostatModes.Off,
                HvacModes.Heat => ThermostatModes.Heat,
                HvacModes.Cool => ThermostatModes.Cool,
                HvacModes.HeatCool or HvacModes.Auto => ThermostatModes.Auto,
                _ => null
            }).Where(w => w is not null)
            .Cast<string>()
            .Distinct()];

    /// <summary>
    /// Builds the supported-property list for the ThermostatController
    /// capability so it matches what the state transformer can report for
    /// this entity.
    /// </summary>
    /// <remarks>
    /// ThermostatMode is always advertised. The setpoint shape is
    /// mutually exclusive — dual setpoints when
    /// <c>TARGET_TEMPERATURE_RANGE</c> is set, otherwise a single
    /// setpoint — to keep the state report self-consistent. Advertising
    /// both shapes at once forced the bridge to either invent missing
    /// values or violate lockstep at state-report time.
    /// </remarks>
    /// <param name="features">The HA <c>supported_features</c> bitmask.</param>
    /// <returns>The thermostat property names to advertise.</returns>
    private static IEnumerable<CapabilityPropertyName> BuildThermostatPropertyList(int features)
    {
        yield return new(ThermostatControllerProperties.ThermostatMode);

        if ((features & ClimateFeatures.TargetTemperatureRange) != 0)
        {
            yield return new(ThermostatControllerProperties.LowerSetpoint);
            yield return new(ThermostatControllerProperties.UpperSetpoint);
        }
        else
        {
            yield return new(ThermostatControllerProperties.TargetSetpoint);
        }
    }
}
