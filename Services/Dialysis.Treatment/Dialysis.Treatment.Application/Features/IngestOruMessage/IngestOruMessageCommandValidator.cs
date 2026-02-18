using Verifier;

namespace Dialysis.Treatment.Application.Features.IngestOruMessage;

public sealed class IngestOruMessageCommandValidator : AbstractValidator<IngestOruMessageCommand>
{
    public IngestOruMessageCommandValidator()
    {
        _ = RuleFor(x => x.RawHl7Message)
            .NotEmpty("RawHl7Message is required.")
            .Must(m => m.Contains("ORU^R01") || m.Contains("ORU|R01"), "Message must be ORU^R01 (PCD-01) type.");
    }
}
