using Verifier;

namespace Dialysis.Alarm.Application.Features.IngestOruR40Message;

public sealed class IngestOruR40MessageCommandValidator : AbstractValidator<IngestOruR40MessageCommand>
{
    public IngestOruR40MessageCommandValidator()
    {
        _ = RuleFor(x => x.RawHl7Message)
            .NotEmpty("RawHl7Message is required.")
            .Must(m => m.Contains("ORU^R40") || m.Contains("ORU|R40"), "Message must be ORU^R40 (PCD-04) type.");
    }
}
