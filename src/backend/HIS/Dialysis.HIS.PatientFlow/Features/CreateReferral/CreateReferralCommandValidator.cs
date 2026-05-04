using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.PatientFlow.Features.CreateReferral;

public sealed class CreateReferralCommandValidator : AbstractValidator<CreateReferralCommand>
{
    public CreateReferralCommandValidator()
    {
        RuleFor(static c => c.ReferralTypeCode, nameof(CreateReferralCommand.ReferralTypeCode)).NotEmpty();
    }
}
