using Verifier;

namespace Dialysis.Treatment.Application.Features.RecordObservation;

public sealed class RecordObservationCommandValidator : AbstractValidator<RecordObservationCommand>
{
    public RecordObservationCommandValidator()
    {
        _ = RuleFor(x => x.SessionId)
            .NotEmpty("SessionId is required.");

        _ = RuleFor(x => x.Observations)
            .NotNull()
            .Must(o => o is not null && o.Count > 0, "At least one observation is required.");
    }
}
