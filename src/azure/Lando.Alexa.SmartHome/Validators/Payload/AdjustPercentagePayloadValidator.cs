using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.PercentageController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates <see cref="AdjustPercentagePayload"/>: requires the
/// <see cref="AdjustPercentagePayload.PercentageDelta"/> to fall in the
/// <c>-100..100</c> band Alexa documents for the directive.
/// </summary>
/// <remarks>
/// Used by covers and fans that still surface PercentageController for
/// legacy compatibility with cached Alexa endpoints. Anything outside the
/// band is a spec violation.
/// </remarks>
public class AdjustPercentagePayloadValidator : AbstractValidator<AdjustPercentagePayload>
{
    /// <summary>
    /// Configures the percentage-delta range rule that
    /// <c>Validate</c> applies to inbound
    /// <c>AdjustPercentage</c> payloads.
    /// </summary>
    /// <remarks>
    /// Single rule: <c>percentageDelta</c> within <c>-100..100</c>.
    /// </remarks>
    public AdjustPercentagePayloadValidator()
    {
        RuleFor(p => p.PercentageDelta)
            .InclusiveBetween(-100, 100)
            .WithMessage("PercentageDelta must be between -100 and 100");
    }
}
