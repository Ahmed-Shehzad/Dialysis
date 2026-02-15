using Verifier;
using Verifier.Abstractions;

namespace Dialysis.AuditConsent.Features.Audit;

public sealed class RecordAuditValidator : AbstractValidator<RecordAuditCommand>
{
    public RecordAuditValidator()
    {
        RuleFor(x => x.ResourceType).NotEmpty("Resource type is required.");
        RuleFor(x => x.ResourceId).NotEmpty("Resource ID is required.");
        RuleFor(x => x.Action).NotEmpty("Action is required.");
    }
}
