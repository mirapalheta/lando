using Lando.Alexa.SmartHome.Models.Authorization;
using Lando.Alexa.SmartHome.Models.Interfaces.BrightnessController;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorController;
using Lando.Alexa.SmartHome.Models.Interfaces.ColorTemperatureController;
using Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;
using Lando.Alexa.SmartHome.Validators.Payload;

namespace Lando.Alexa.SmartHome.Validators.Tests;

/// <summary>
/// Sanity tests for the per-payload <c>AbstractValidator</c> implementations.
/// Each validator is small (1–3 rules), so one test per rule branch is
/// enough to pin the contract — these aren't designed to catch fancy
/// FluentValidation edge cases, just the per-payload range/structure rules
/// the bridge actually depends on.
/// </summary>
/// <remarks>
/// Validators that take no rules (<see cref="EmptyPayloadValidator"/>,
/// <see cref="ResumeSchedulePayloadValidator"/>,
/// <see cref="SetMutePayloadValidator"/>) are exercised via a trivial
/// always-valid assertion to keep the registration surface covered.
/// </remarks>
public class PayloadValidatorTests
{
    // ---------- BrightnessController ----------

    [Theory]
    [InlineData(0, true)]
    [InlineData(50, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void SetBrightness_validates_inclusive_band(int value, bool valid)
        => new SetBrightnessPayloadValidator().Validate(new SetBrightnessPayload { Brightness = value }).IsValid.ShouldBe(valid);

    [Theory]
    [InlineData(-100, true)]
    [InlineData(100, true)]
    [InlineData(-101, false)]
    [InlineData(101, false)]
    public void AdjustBrightness_validates_inclusive_delta_band(int value, bool valid)
        => new AdjustBrightnessPayloadValidator().Validate(new AdjustBrightnessPayload { BrightnessDelta = value }).IsValid.ShouldBe(valid);

    // ---------- PercentageController ----------

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void SetPercentage_validates_inclusive_band(int value, bool valid)
        => new SetPercentagePayloadValidator().Validate(new SetPercentagePayload { Percentage = value }).IsValid.ShouldBe(valid);

    [Theory]
    [InlineData(-100, true)]
    [InlineData(100, true)]
    [InlineData(-101, false)]
    [InlineData(101, false)]
    public void AdjustPercentage_validates_inclusive_delta_band(int value, bool valid)
        => new AdjustPercentagePayloadValidator().Validate(new AdjustPercentagePayload { PercentageDelta = value }).IsValid.ShouldBe(valid);

    // ---------- RangeController ----------

    [Theory]
    [InlineData(0d, true)]
    [InlineData(100d, true)]
    [InlineData(-0.1, false)]
    [InlineData(100.1, false)]
    public void SetRangeValue_validates_inclusive_band(double value, bool valid)
        => new SetRangeValuePayloadValidator().Validate(new SetRangeValuePayload { RangeValue = value }).IsValid.ShouldBe(valid);

    [Theory]
    [InlineData(-100d, true)]
    [InlineData(100d, true)]
    [InlineData(-100.1, false)]
    [InlineData(100.1, false)]
    public void AdjustRangeValue_validates_inclusive_delta_band(double value, bool valid)
        => new AdjustRangeValuePayloadValidator().Validate(new AdjustRangeValuePayload { RangeValueDelta = value }).IsValid.ShouldBe(valid);

    // ---------- ColorController ----------

    [Fact]
    public void SetColor_rejects_null_color()
        => new SetColorPayloadValidator().Validate(new SetColorPayload { Color = null! }).IsValid.ShouldBeFalse();

    [Theory]
    [InlineData(0d, 0d, 0d, true)]
    [InlineData(180d, 0.5, 0.5, true)]
    [InlineData(360d, 1d, 1d, true)]
    [InlineData(-1d, 0.5, 0.5, false)]
    [InlineData(361d, 0.5, 0.5, false)]
    [InlineData(180d, -0.1, 0.5, false)]
    [InlineData(180d, 1.1, 0.5, false)]
    [InlineData(180d, 0.5, -0.1, false)]
    [InlineData(180d, 0.5, 1.1, false)]
    public void SetColor_validates_hsb_bands(double hue, double saturation, double brightness, bool valid)
    {
        var payload = new SetColorPayload { Color = new HsbColor { Hue = hue, Saturation = saturation, Brightness = brightness } };
        new SetColorPayloadValidator().Validate(payload).IsValid.ShouldBe(valid);
    }

    // ---------- ColorTemperatureController ----------

    [Theory]
    [InlineData(1000, true)]
    [InlineData(10000, true)]
    [InlineData(999, false)]
    [InlineData(10001, false)]
    public void SetColorTemperature_validates_inclusive_band(int kelvin, bool valid)
        => new SetColorTemperaturePayloadValidator().Validate(new SetColorTemperaturePayload { ColorTemperatureInKelvin = kelvin }).IsValid.ShouldBe(valid);

    // ---------- Speaker ----------

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void SetVolume_validates_inclusive_band(int value, bool valid)
        => new SetVolumePayloadValidator().Validate(new SetVolumePayload { Volume = value }).IsValid.ShouldBe(valid);

    [Theory]
    [InlineData(-100, true)]
    [InlineData(100, true)]
    [InlineData(-101, false)]
    [InlineData(101, false)]
    public void AdjustVolume_validates_inclusive_delta_band(int value, bool valid)
        => new AdjustVolumePayloadValidator().Validate(new AdjustVolumePayload { Volume = value }).IsValid.ShouldBe(valid);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetMute_accepts_any_bool(bool value)
        => new SetMutePayloadValidator().Validate(new SetMutePayload { Mute = value }).IsValid.ShouldBeTrue();

    // ---------- ThermostatController ----------

    [Fact]
    public void AdjustTargetTemperature_requires_delta()
        => new AdjustTargetTemperaturePayloadValidator().Validate(new AdjustTargetTemperaturePayload { TargetSetpointDelta = null! }).IsValid.ShouldBeFalse();

    [Theory]
    [InlineData(TemperatureScale.Celsius, true)]
    [InlineData(TemperatureScale.Fahrenheit, true)]
    [InlineData(TemperatureScale.Kelvin, true)]
    [InlineData("RANKINE", false)]
    [InlineData("", false)]
    public void AdjustTargetTemperature_validates_scale(string scale, bool valid)
    {
        var payload = new AdjustTargetTemperaturePayload { TargetSetpointDelta = new Temperature { Value = 2, Scale = scale } };
        new AdjustTargetTemperaturePayloadValidator().Validate(payload).IsValid.ShouldBe(valid);
    }

    [Fact]
    public void SetTargetTemperature_rejects_payload_with_no_setpoints()
        => new SetTargetTemperaturePayloadValidator().Validate(new SetTargetTemperaturePayload()).IsValid.ShouldBeFalse();

    [Fact]
    public void SetTargetTemperature_accepts_single_setpoint()
    {
        var payload = new SetTargetTemperaturePayload { TargetSetpoint = new Temperature { Value = 72, Scale = TemperatureScale.Fahrenheit } };
        new SetTargetTemperaturePayloadValidator().Validate(payload).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetTargetTemperature_accepts_balanced_range()
    {
        var payload = new SetTargetTemperaturePayload
        {
            LowerSetpoint = new Temperature { Value = 68, Scale = TemperatureScale.Fahrenheit },
            UpperSetpoint = new Temperature { Value = 75, Scale = TemperatureScale.Fahrenheit },
        };
        new SetTargetTemperaturePayloadValidator().Validate(payload).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetTargetTemperature_rejects_unbalanced_range()
    {
        var payload = new SetTargetTemperaturePayload
        {
            LowerSetpoint = new Temperature { Value = 68, Scale = TemperatureScale.Fahrenheit },
        };
        new SetTargetTemperaturePayloadValidator().Validate(payload).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void SetTargetTemperature_rejects_inverted_range()
    {
        var payload = new SetTargetTemperaturePayload
        {
            LowerSetpoint = new Temperature { Value = 80, Scale = TemperatureScale.Fahrenheit },
            UpperSetpoint = new Temperature { Value = 70, Scale = TemperatureScale.Fahrenheit },
        };
        new SetTargetTemperaturePayloadValidator().Validate(payload).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(ThermostatModes.Heat, true)]
    [InlineData("FAN_ONLY", true)] // unknown but non-empty → forwarded to HA
    public void SetThermostatMode_requires_non_empty_value(string value, bool valid)
    {
        var payload = new SetThermostatModePayload { ThermostatMode = new ThermostatMode { Value = value } };
        new SetThermostatModePayloadValidator().Validate(payload).IsValid.ShouldBe(valid);
    }

    [Fact]
    public void SetThermostatMode_custom_requires_custom_name()
    {
        var payload = new SetThermostatModePayload
        {
            ThermostatMode = new ThermostatMode { Value = ThermostatModes.Custom, CustomName = null },
        };
        new SetThermostatModePayloadValidator().Validate(payload).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void SetThermostatMode_custom_with_custom_name_passes()
    {
        var payload = new SetThermostatModePayload
        {
            ThermostatMode = new ThermostatMode { Value = ThermostatModes.Custom, CustomName = "boost" },
        };
        new SetThermostatModePayloadValidator().Validate(payload).IsValid.ShouldBeTrue();
    }

    // ---------- AcceptGrant ----------

    [Fact]
    public void AcceptGrant_requires_grant_and_grantee_fields()
    {
        var payload = new AcceptGrantPayload(); // default ctor leaves Grant.Code / Grantee.Token empty
        new AcceptGrantPayloadValidator().Validate(payload).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void AcceptGrant_valid_payload_passes()
    {
        var payload = new AcceptGrantPayload
        {
            Grant = new Grant { Type = GrantType.OAuth2AuthorizationCode, Code = "the-code" },
            Grantee = new Grantee { Type = "BearerToken", Token = new SecureString("bearer") },
        };
        new AcceptGrantPayloadValidator().Validate(payload).IsValid.ShouldBeTrue();
    }
}
