using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.Identity.Provisioning.Features.ProvisionUser;

/// <summary>
/// Validates user-provisioning input before an account is created. Subject + display name are
/// required identity anchors; email, when supplied, must look like an address.
/// </summary>
public sealed class ProvisionUserCommandValidator : AbstractValidator<ProvisionUserCommand>
{
    public ProvisionUserCommandValidator()
    {
        RuleFor(static c => c.Subject, nameof(ProvisionUserCommand.Subject))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 256)
            .WithMessage("Subject is required and must be at most 256 characters.");

        RuleFor(static c => c.DisplayName, nameof(ProvisionUserCommand.DisplayName))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 256)
            .WithMessage("DisplayName is required and must be at most 256 characters.");

        RuleFor(static c => c.Email, nameof(ProvisionUserCommand.Email))
            .Must(static (_, v) => v is null || (v.Length <= 256 && LooksLikeEmail(v)))
            .WithMessage("Email, when provided, must be a valid address of at most 256 characters.");
    }

    private static bool LooksLikeEmail(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < value.Length - 1 && value.IndexOf('@', at + 1) < 0;
    }
}
