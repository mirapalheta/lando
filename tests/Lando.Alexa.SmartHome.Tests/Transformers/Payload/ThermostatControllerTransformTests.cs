using Lando.Alexa.SmartHome.Models.ErrorResponse;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

namespace Lando.Alexa.SmartHome.Transformers.Payload.Tests;

/// <summary>
/// ThermostatController transforms. Tests pin: dual vs single setpoint
/// dispatch on <see cref="SetTargetTemperaturePayloadTransform"/>; mode
/// translation (Alexa upper-case → HA lower-case canonical) on
/// <see cref="SetThermostatModePayloadTransform"/>; and the delta-against-
/// current-setpoint math on <see cref="AdjustTargetTemperaturePayloadTransform"/>.
/// </summary>
public class ThermostatControllerTransformTests
{
    [Fact]
    public void SetTargetTemperature_dual_setpoint_emits_low_and_high()
    {
        var entity = TestEntities.Climate();
        var payload = new SetTargetTemperaturePayload
        {
            LowerSetpoint = new Temperature { Value = 68 },
            UpperSetpoint = new Temperature { Value = 75 },
        };

        var request = new SetTargetTemperaturePayloadTransform().Transform(entity, payload);

        request.Service.ShouldBe("set_temperature");
        request.TargetTempLow.ShouldBe(68);
        request.TargetTempHigh.ShouldBe(75);
        request.Temperature.ShouldBeNull();
    }

    [Fact]
    public void SetTargetTemperature_single_setpoint_emits_temperature()
    {
        var entity = TestEntities.Climate();
        var payload = new SetTargetTemperaturePayload { TargetSetpoint = new Temperature { Value = 72 } };

        var request = new SetTargetTemperaturePayloadTransform().Transform(entity, payload);

        request.Temperature.ShouldBe(72);
        request.TargetTempLow.ShouldBeNull();
        request.TargetTempHigh.ShouldBeNull();
    }

    [Fact]
    public void SetTargetTemperature_with_no_setpoint_throws_InvalidValue()
    {
        var entity = TestEntities.Climate();
        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new SetTargetTemperaturePayloadTransform().Transform(entity, new SetTargetTemperaturePayload()));
        ex.Error.ShouldBe(ErrorType.InvalidValue);
    }

    [Theory]
    [InlineData(ThermostatModes.Off, "off")]
    [InlineData(ThermostatModes.Heat, "heat")]
    [InlineData(ThermostatModes.EmergencyHeat, "heat")]
    [InlineData(ThermostatModes.Cool, "cool")]
    [InlineData(ThermostatModes.Auto, "heat_cool")]
    [InlineData(ThermostatModes.EcoMode, "auto")]
    public void SetThermostatMode_translates_canonical_modes(string alexa, string ha)
    {
        var entity = TestEntities.Climate();
        var payload = new SetThermostatModePayload { ThermostatMode = new ThermostatMode { Value = alexa } };
        var request = new SetThermostatModePayloadTransform().Transform(entity, payload);

        request.Service.ShouldBe("set_hvac_mode");
        request.HvacMode.ShouldBe(ha);
    }

    [Fact]
    public void SetThermostatMode_custom_forwards_custom_name()
    {
        var entity = TestEntities.Climate();
        var payload = new SetThermostatModePayload
        {
            ThermostatMode = new ThermostatMode { Value = ThermostatModes.Custom, CustomName = "boost" },
        };

        var request = new SetThermostatModePayloadTransform().Transform(entity, payload);

        request.HvacMode.ShouldBe("boost");
    }

    [Fact]
    public void SetThermostatMode_custom_with_null_name_falls_back_to_auto()
    {
        var entity = TestEntities.Climate();
        var payload = new SetThermostatModePayload
        {
            ThermostatMode = new ThermostatMode { Value = ThermostatModes.Custom, CustomName = null },
        };

        var request = new SetThermostatModePayloadTransform().Transform(entity, payload);

        request.HvacMode.ShouldBe("auto");
    }

    [Fact]
    public void SetThermostatMode_unknown_value_lowercases()
    {
        var entity = TestEntities.Climate();
        var payload = new SetThermostatModePayload { ThermostatMode = new ThermostatMode { Value = "FAN_ONLY" } };
        var request = new SetThermostatModePayloadTransform().Transform(entity, payload);

        request.HvacMode.ShouldBe("fan_only");
    }

    [Fact]
    public void AdjustTargetTemperature_applies_delta_to_current_setpoint()
    {
        var entity = TestEntities.Climate(targetTemp: 70);
        var payload = new AdjustTargetTemperaturePayload { TargetSetpointDelta = new Temperature { Value = 2 } };

        var request = new AdjustTargetTemperaturePayloadTransform().Transform(entity, payload);

        request.Service.ShouldBe("set_temperature");
        request.Temperature.ShouldBe(72);
    }

    [Fact]
    public void AdjustTargetTemperature_without_current_setpoint_throws_InvalidValue()
    {
        // No target temp attribute on the entity.
        var entity = TestEntities.Climate(targetTemp: null);
        var payload = new AdjustTargetTemperaturePayload { TargetSetpointDelta = new Temperature { Value = 2 } };

        var ex = Should.Throw<AlexaSmartHomeException>(
            () => new AdjustTargetTemperaturePayloadTransform().Transform(entity, payload));
        ex.Error.ShouldBe(ErrorType.InvalidValue);
    }
}
