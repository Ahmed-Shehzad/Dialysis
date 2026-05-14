using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterFinancialErpLink;

public sealed class RegisterFinancialErpLinkCommandValidator : AbstractValidator<RegisterFinancialErpLinkCommand>
{
    private static readonly HashSet<string> _allowedStatuses = new(StringComparer.Ordinal)
    {
        "Active",
        "Inactive",
        "Failing",
        "PendingHandshake",
    };

    public RegisterFinancialErpLinkCommandValidator()
    {
        RuleFor(static c => c.SystemCode, nameof(RegisterFinancialErpLinkCommand.SystemCode))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 64)
            .WithMessage("SystemCode is required (max 64 characters).");

        RuleFor(static c => c.StatusCode, nameof(RegisterFinancialErpLinkCommand.StatusCode))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && _allowedStatuses.Contains(v))
            .WithMessage("StatusCode must be one of: Active, Inactive, Failing, PendingHandshake.");
    }
}
