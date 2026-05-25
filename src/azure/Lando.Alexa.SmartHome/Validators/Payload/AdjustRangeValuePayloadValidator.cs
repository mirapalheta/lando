using FluentValidation;
using Lando.Alexa.SmartHome.Models.Interfaces.RangeController;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates <see cref="AdjustRangeValuePayload"/>: requires the
/// <see cref="AdjustRangeValuePayload.RangeValueDelta"/> to fall in the
/// <c>-100..100</c> band that matches the supported range advertised by
/// every RangeController instance the bridge currently emits.
/// </summary>
/// <remarks>
/// The bridge advertises a 0..100 range for both shade position and fan
/// speed, so a delta outside <c>-100..100</c> can never land on a valid
/// target value regardless of the current position.
/// </remarks>
public class AdjustRangeValuePayloadValidator : AbstractValidator<AdjustRangeValuePayload>
{
    /// <summary>
    /// Configures the range-delta rule that <c>Validate</c>
    /// applies to inbound <c>AdjustRangeValue</c> payloads.
    /// </summary>
    /// <remarks>
    /// Single rule: <c>rangeValueDelta</c> within <c>-100..100</c>.
    /// </remarks>
    public AdjustRangeValuePayloadValidator()
    {
        RuleFor(p => p.RangeValueDelta)
            .InclusiveBetween(-100, 100)
            .WithMessage("RangeValueDelta must be between -100 and 100");
    }
}
