using Verifier;

namespace Dialysis.HisIntegration.Features.AdtSync;

public sealed class AdtIngestValidator : AbstractValidator<AdtIngestCommand>
{
    public AdtIngestValidator()
    {
        RuleFor(x => x.MessageType).NotEmpty("Message type is required.");
        RuleFor(x => x.RawMessage).NotEmpty("Raw ADT message is required.");
    }
}
