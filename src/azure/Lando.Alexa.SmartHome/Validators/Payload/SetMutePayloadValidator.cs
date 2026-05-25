using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.Speaker;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validator for <see cref="SetMutePayload"/>. No rules are applied — the
/// payload's single field is a boolean and both values are spec-valid —
/// but a concrete validator is still registered so the directive
/// handler's <c>IValidator&lt;SetMutePayload&gt;</c> dependency resolves
/// without forcing the handler to special-case nulls.
/// </summary>
/// <remarks>
/// Following the same convention as
/// <see cref="EmptyPayloadValidator"/>: register the no-op validator
/// explicitly rather than special-casing the handler.
/// </remarks>
public class SetMutePayloadValidator : AbstractValidator<SetMutePayload>
{
}
