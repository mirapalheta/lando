using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates <see cref="SetTargetTemperaturePayload"/>: requires either a
/// single <see cref="SetTargetTemperaturePayload.TargetSetpoint"/> or both
/// of (<see cref="SetTargetTemperaturePayload.LowerSetpoint"/>,
/// <see cref="SetTargetTemperaturePayload.UpperSetpoint"/>) — the two
/// shapes Alexa actually sends.
/// </summary>
/// <remarks>
/// Also enforces a recognised <see cref="Temperature.Scale"/> on each
/// supplied setpoint. Specific value bounds are intentionally not
/// enforced; HA validates against the entity's <c>min_temp</c> and
/// <c>max_temp</c> attributes and rejects out-of-range requests with a
/// proper service error.
/// </remarks>
public class SetTargetTemperaturePayloadValidator : AbstractValidator<SetTargetTemperaturePayload>
{
    /// <summary>
    /// Configures the rules that <c>Validate</c> applies to
    /// inbound <c>SetTargetTemperature</c> payloads.
    /// </summary>
    /// <remarks>
    /// Rules: at least one of the three setpoints must be present; if dual
    /// setpoints are sent, both <c>lowerSetpoint</c> and <c>upperSetpoint</c>
    /// must be present; each supplied setpoint must carry a recognised
    /// scale; and in dual-setpoint mode the lower must be less than or
    /// equal to the upper.
    /// </remarks>
    public SetTargetTemperaturePayloadValidator()
    {
        RuleFor(p => p)
            .Must(HaveAtLeastOneSetpoint)
            .WithMessage("At least one of targetSetpoint, lowerSetpoint, or upperSetpoint is required");

        RuleFor(p => p)
            .Must(HaveBothRangeBoundsWhenEitherIsPresent)
            .WithMessage("lowerSetpoint and upperSetpoint must both be supplied when either is present");

        RuleFor(p => p)
            .Must(LowerNotAboveUpper)
            .When(p => p.LowerSetpoint is not null && p.UpperSetpoint is not null)
            .WithMessage("lowerSetpoint must be less than or equal to upperSetpoint");

        RuleFor(p => p.TargetSetpoint!.Scale)
            .Must(BeValidScale)
            .When(p => p.TargetSetpoint is not null)
            .WithMessage($"Scale must be one of {TemperatureScale.Celsius}, {TemperatureScale.Fahrenheit}, {TemperatureScale.Kelvin}");

        RuleFor(p => p.LowerSetpoint!.Scale)
            .Must(BeValidScale)
            .When(p => p.LowerSetpoint is not null)
            .WithMessage($"Scale must be one of {TemperatureScale.Celsius}, {TemperatureScale.Fahrenheit}, {TemperatureScale.Kelvin}");

        RuleFor(p => p.UpperSetpoint!.Scale)
            .Must(BeValidScale)
            .When(p => p.UpperSetpoint is not null)
            .WithMessage($"Scale must be one of {TemperatureScale.Celsius}, {TemperatureScale.Fahrenheit}, {TemperatureScale.Kelvin}");
    }

    private static bool HaveAtLeastOneSetpoint(SetTargetTemperaturePayload p)
        => p.TargetSetpoint is not null || p.LowerSetpoint is not null || p.UpperSetpoint is not null;

    private static bool HaveBothRangeBoundsWhenEitherIsPresent(SetTargetTemperaturePayload p)
        => (p.LowerSetpoint is null) == (p.UpperSetpoint is null);

    private static bool LowerNotAboveUpper(SetTargetTemperaturePayload p)
        => p.LowerSetpoint!.Value <= p.UpperSetpoint!.Value;

    private static bool BeValidScale(string scale)
        => scale is TemperatureScale.Celsius
            or TemperatureScale.Fahrenheit
            or TemperatureScale.Kelvin;
}
