using System;
using System.Collections.Generic;
using Lando.Alexa.SmartHome.Models.Core;
using Lando.Alexa.SmartHome.Models.Interfaces.TemperatureSensor;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.HomeAssistant.Models;

namespace Lando.Alexa.SmartHome.Transformers.Entity;

using static Lando.HomeAssistant.Constants;

/// <summary>
/// State transformer for the <c>climate</c> HA domain. Reports current
/// temperature, the matching setpoint shape (single or dual), and the
/// current thermostat mode, in lockstep with what
/// <see cref="ClimateDiscoveryTransformer"/> advertised.
/// </summary>
/// <remarks>
/// Lockstep matters: Alexa marks the endpoint unhealthy (and the app shows
/// "Device is unresponsive" / a perpetual "Waiting for Home Assistant…"
/// spinner) when a retrievable property advertised at discovery time has
/// no corresponding value in the state report. To prevent that, this
/// transformer emits a property for every shape the discovery transformer
/// is allowed to advertise — falling back to a neutral value when HA's
/// own attribute is null.
/// </remarks>
public class ClimateStateTransformer : StateTransformerBase
{
    /// <inheritdoc />
    protected override IEnumerable<ContextProperty> GetDomainProperties(HomeAssistantEntity entity)
    {
        var scale = ResolveTemperatureScale(entity.GetUnitOfMeasurement());
        var features = entity.GetSupportedFeatures();
        var attrs = entity.Attributes;

        var current = attrs.GetDouble(EntityAttributes.CurrentTemperature);
        var target = attrs.GetDouble(EntityAttributes.Temperature);
        var low = attrs.GetDouble(EntityAttributes.TargetTempLow);
        var high = attrs.GetDouble(EntityAttributes.TargetTempHigh);

        // TemperatureSensor.temperature is always advertised; emit a
        // best-effort value so Alexa never sees a missing-property report.
        // Falls back to the target setpoint, then to a neutral 0 — the
        // only path that produces 0 is a thermostat with no readings at
        // all, where the connectivity property will already be Unreachable.
        yield return new ContextProperty
        {
            Namespace = Namespaces.TemperatureSensor,
            Name = TemperatureSensorProperties.Temperature,
            Value = new Temperature
            {
                Value = current ?? target ?? low ?? high ?? 0,
                Scale = scale
            },
            TimeOfSample = entity.LastUpdated,
            UncertaintyInMilliseconds = DefaultUncertaintyMs
        };

        // Match the setpoint shape the discovery transformer advertised:
        // dual when TARGET_TEMPERATURE_RANGE is supported, single otherwise.
        if ((features & ClimateFeatures.TargetTemperatureRange) != 0)
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.ThermostatController,
                Name = ThermostatControllerProperties.LowerSetpoint,
                Value = new Temperature { Value = low ?? target ?? 0, Scale = scale },
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };

            yield return new ContextProperty
            {
                Namespace = Namespaces.ThermostatController,
                Name = ThermostatControllerProperties.UpperSetpoint,
                Value = new Temperature { Value = high ?? target ?? 0, Scale = scale },
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
        }
        else
        {
            yield return new ContextProperty
            {
                Namespace = Namespaces.ThermostatController,
                Name = ThermostatControllerProperties.TargetSetpoint,
                Value = new Temperature { Value = target ?? low ?? high ?? 0, Scale = scale },
                TimeOfSample = entity.LastUpdated,
                UncertaintyInMilliseconds = DefaultUncertaintyMs
            };
        }

        yield return new ContextProperty
        {
            Namespace = Namespaces.ThermostatController,
            Name = ThermostatControllerProperties.ThermostatMode,
            Value = MapHvacMode(entity.State),
            TimeOfSample = entity.LastUpdated,
            UncertaintyInMilliseconds = DefaultUncertaintyMs
        };
    }

    /// <summary>
    /// Picks the Alexa temperature scale to report against based on the HA
    /// <c>unit_of_measurement</c> string.
    /// </summary>
    /// <remarks>
    /// HA writes the unit as the literal symbol (for example <c>"°F"</c>);
    /// a naïve equality check would miss formatting differences across
    /// integrations, so the bridge looks for an <c>F</c> anywhere in the
    /// string. Defaults to Fahrenheit when the entity doesn't expose a unit
    /// — most US-based HA installations are Fahrenheit, and the
    /// fallback only matters when HA forgot to set a unit at all.
    /// </remarks>
    /// <param name="entityUnit">The HA <c>unit_of_measurement</c> string.</param>
    /// <param name="fallbackUnit">The unit to use when the entity omits one.</param>
    /// <returns>
    /// <see cref="TemperatureScale.Fahrenheit"/> when the resolved unit
    /// contains <c>F</c>; otherwise <see cref="TemperatureScale.Celsius"/>.
    /// </returns>
    private static string ResolveTemperatureScale(string? entityUnit, string fallbackUnit = "°F")
        => (entityUnit ?? fallbackUnit).Contains('F', StringComparison.OrdinalIgnoreCase)
            ? TemperatureScale.Fahrenheit
            : TemperatureScale.Celsius;

    /// <summary>
    /// Maps the HA hvac state string onto the canonical Alexa
    /// <see cref="ThermostatMode"/>.
    /// </summary>
    /// <remarks>
    /// Unmapped values fall back to <see cref="ThermostatModes.Auto"/>
    /// rather than throwing — a thermostat reporting an unknown mode
    /// should still surface as a controllable endpoint.
    /// </remarks>
    /// <param name="state">The HA entity state string.</param>
    /// <returns>The matching Alexa thermostat mode.</returns>
    private static string MapHvacMode(string? state)
        => state switch
        {
            "heat" => ThermostatModes.Heat,
            "cool" => ThermostatModes.Cool,
            "heat_cool" or "auto" => ThermostatModes.Auto,
            "off" => ThermostatModes.Off,
            _ => ThermostatModes.Auto
        };
}
