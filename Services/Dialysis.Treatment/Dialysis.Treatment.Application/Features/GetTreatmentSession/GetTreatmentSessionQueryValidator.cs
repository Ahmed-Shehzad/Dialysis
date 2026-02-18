using Verifier;

namespace Dialysis.Treatment.Application.Features.GetTreatmentSession;

/// <summary>
/// Validates <see cref="GetTreatmentSessionQuery"/>.
/// </summary>
internal sealed class GetTreatmentSessionQueryValidator : AbstractValidator<GetTreatmentSessionQuery>
{
    public GetTreatmentSessionQueryValidator()
    {
        _ = RuleFor(x => x.SessionId)
            .NotEmpty("SessionId is required.");
    }
}
