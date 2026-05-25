using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates <see cref="SetThermostatModePayload"/>: requires a non-empty
/// <see cref="ThermostatMode.Value"/> and, when the value is
/// <see cref="ThermostatModes.Custom"/>, requires a
/// <see cref="ThermostatMode.CustomName"/> to disambiguate the integration
/// specific mode being requested.
/// </summary>
/// <remarks>
/// Beyond those two constraints the value is intentionally not restricted to
/// the canonical <see cref="ThermostatModes"/> set — Alexa allows custom
/// modes and the bridge forwards them verbatim to Home Assistant, so the
/// validator must not reject unknown strings that the customer's
/// integration may legitimately accept.
/// </remarks>
public class SetThermostatModePayloadValidator : AbstractValidator<SetThermostatModePayload>
{
    /// <summary>
    /// Configures the rules that <c>Validate</c> applies to
    /// inbound <c>SetThermostatMode</c> payloads.
    /// </summary>
    /// <remarks>
    /// Two rules: (a) the <c>value</c> field must be a non-empty string,
    /// otherwise the directive has no target to set; (b) when the value is
    /// <c>CUSTOM</c>, the companion <c>customName</c> must be supplied or
    /// the bridge has no idea which HA mode the customer means.
    /// </remarks>
    public SetThermostatModePayloadValidator()
    {
        RuleFor(p => p.ThermostatMode).NotNull().WithMessage("ThermostatMode is required");
        RuleFor(p => p.ThermostatMode!.Value)
            .NotEmpty()
            .When(p => p.ThermostatMode is not null)
            .WithMessage("ThermostatMode.Value is required");
        RuleFor(p => p.ThermostatMode!.CustomName)
            .NotEmpty()
            .When(p => p.ThermostatMode is not null && p.ThermostatMode.Value == ThermostatModes.Custom)
            .WithMessage("ThermostatMode.CustomName is required when Value is CUSTOM");
    }
}
