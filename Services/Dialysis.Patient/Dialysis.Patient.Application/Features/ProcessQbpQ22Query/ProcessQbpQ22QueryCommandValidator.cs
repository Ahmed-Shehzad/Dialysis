using Verifier;

namespace Dialysis.Patient.Application.Features.ProcessQbpQ22Query;

internal sealed class ProcessQbpQ22QueryCommandValidator : AbstractValidator<ProcessQbpQ22QueryCommand>
{
    public ProcessQbpQ22QueryCommandValidator()
    {
        _ = RuleFor(x => x.RawHl7Message)
            .NotEmpty("RawHl7Message is required.")
            .Must(m => m.Contains("QBP^Q22") || m.Contains("QBP|Q22"), "Message must be QBP^Q22 (PDQ) type.");
    }
}
