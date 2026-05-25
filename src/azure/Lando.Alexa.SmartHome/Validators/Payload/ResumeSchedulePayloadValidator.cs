using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.ThermostatController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validator for <see cref="ResumeSchedulePayload"/>. No rules are
/// applied — the payload is empty by spec — but a concrete validator is
/// still registered so the directive handler's
/// <c>IValidator&lt;ResumeSchedulePayload&gt;</c> dependency resolves
/// without forcing the handler to special-case nulls.
/// </summary>
/// <remarks>
/// Mirrors the no-op convention used by
/// <see cref="EmptyPayloadValidator"/>.
/// </remarks>
public class ResumeSchedulePayloadValidator : AbstractValidator<ResumeSchedulePayload>
{
}
