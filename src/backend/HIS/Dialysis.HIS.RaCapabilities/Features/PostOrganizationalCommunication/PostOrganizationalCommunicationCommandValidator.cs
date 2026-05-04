using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.PostOrganizationalCommunication;

public sealed class PostOrganizationalCommunicationCommandValidator : AbstractValidator<PostOrganizationalCommunicationCommand>
{
    public PostOrganizationalCommunicationCommandValidator()
    {
        RuleFor(static c => c.ThreadCode, nameof(PostOrganizationalCommunicationCommand.ThreadCode))
            .Must(static (_, t) => !string.IsNullOrWhiteSpace(t) && t.Length <= 64)
            .WithMessage("ThreadCode is required (max 64 characters).");
        RuleFor(static c => c.Subject, nameof(PostOrganizationalCommunicationCommand.Subject))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Length <= 256)
            .WithMessage("Subject is required (max 256 characters).");
        RuleFor(static c => c.Body, nameof(PostOrganizationalCommunicationCommand.Body))
            .Must(static (_, b) => !string.IsNullOrWhiteSpace(b) && b.Length <= 8000)
            .WithMessage("Body is required (max 8000 characters).");
    }
}
