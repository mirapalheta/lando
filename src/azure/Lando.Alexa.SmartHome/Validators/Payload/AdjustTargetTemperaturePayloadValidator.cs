using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates <see cref="AdjustTargetTemperaturePayload"/>: requires the
/// <see cref="AdjustTargetTemperaturePayload.TargetSetpointDelta"/> object
/// to be present and to carry a recognised
/// <see cref="Temperature.Scale"/>.
/// </summary>
/// <remarks>
/// Magnitude is intentionally left unconstrained — a "raise by 10 degrees"
/// command is a valid user intent — but a missing delta or a scale Alexa
/// shouldn't be sending is a spec violation that the handler can't
/// recover from cleanly.
/// </remarks>
public class AdjustTargetTemperaturePayloadValidator : AbstractValidator<AdjustTargetTemperaturePayload>
{
    /// <summary>
    /// Configures the rules that <c>Validate</c> applies to
    /// inbound <c>AdjustTargetTemperature</c> payloads.
    /// </summary>
    /// <remarks>
    /// Two rules: the <c>targetSetpointDelta</c> object must be present,
    /// and its <c>scale</c> must be one of the canonical
    /// <see cref="TemperatureScale"/> values (Celsius, Fahrenheit, Kelvin).
    /// </remarks>
    public AdjustTargetTemperaturePayloadValidator()
    {
        RuleFor(p => p.TargetSetpointDelta).NotNull().WithMessage("TargetSetpointDelta is required");
        RuleFor(p => p.TargetSetpointDelta!.Scale)
            .Must(BeValidScale)
            .When(p => p.TargetSetpointDelta is not null)
            .WithMessage($"Scale must be one of {TemperatureScale.Celsius}, {TemperatureScale.Fahrenheit}, {TemperatureScale.Kelvin}");
    }

    private static bool BeValidScale(string scale)
        => scale is TemperatureScale.Celsius
            or TemperatureScale.Fahrenheit
            or TemperatureScale.Kelvin;
}
