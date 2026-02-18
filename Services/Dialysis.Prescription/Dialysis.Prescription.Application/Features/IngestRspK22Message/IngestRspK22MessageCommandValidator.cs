using Verifier;

namespace Dialysis.Prescription.Application.Features.IngestRspK22Message;

public sealed class IngestRspK22MessageCommandValidator : AbstractValidator<IngestRspK22MessageCommand>
{
    public IngestRspK22MessageCommandValidator()
    {
        _ = RuleFor(x => x.RawHl7Message)
            .NotEmpty("RawHl7Message is required.")
            .Must(m => m.Contains("RSP^K22") || m.Contains("RSP|K22"), "Message must be RSP^K22 prescription response type.");
    }
}
