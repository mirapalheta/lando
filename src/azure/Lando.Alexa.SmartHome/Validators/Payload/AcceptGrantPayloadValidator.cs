using FluentValidation;
using Lando.Alexa.SmartHome.Models.Authorization;

namespace Lando.Alexa.SmartHome.Validators.Payload;

/// <summary>
/// Validates the inbound <see cref="AcceptGrantPayload"/>. Only structural checks live
/// here — the actual code exchange happens in <c>AcceptGrantDirectiveHandler</c>, since
/// a failed exchange must surface as an <c>ACCEPT_GRANT_FAILED</c> error, not a generic
/// validation error.
/// </summary>
public class AcceptGrantPayloadValidator : AbstractValidator<AcceptGrantPayload>
{
    public AcceptGrantPayloadValidator()
    {
        RuleFor(p => p.Grant).NotNull().WithMessage("AcceptGrant.payload.grant is required");
        When(p => p.Grant is not null, () =>
        {
            RuleFor(p => p.Grant.Type).NotEmpty().WithMessage("AcceptGrant.payload.grant.type is required");
            RuleFor(p => p.Grant.Code).NotEmpty().WithMessage("AcceptGrant.payload.grant.code is required");
        });

        RuleFor(p => p.Grantee).NotNull().WithMessage("AcceptGrant.payload.grantee is required");
        When(p => p.Grantee is not null, () =>
        {
            RuleFor(p => p.Grantee.Type).NotEmpty().WithMessage("AcceptGrant.payload.grantee.type is required");
            RuleFor(p => p.Grantee.Token).NotEmpty().WithMessage("AcceptGrant.payload.grantee.token is required");
        });
    }
}
