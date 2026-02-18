using Verifier;

namespace Dialysis.Patient.Application.Features.IngestRspK22;

internal sealed class IngestRspK22CommandValidator : AbstractValidator<IngestRspK22Command>
{
    public IngestRspK22CommandValidator()
    {
        _ = RuleFor(x => x.RawHl7Message)
            .NotEmpty("RawHl7Message is required.")
            .Must(m => m.Contains("RSP^K22") || m.Contains("RSP|K22"), "Message must be RSP^K22 (PDQ response) type.");
    }
}
