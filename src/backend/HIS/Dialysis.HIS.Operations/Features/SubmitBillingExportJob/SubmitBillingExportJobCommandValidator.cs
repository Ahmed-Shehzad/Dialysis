using System.Text.RegularExpressions;
using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Operations.Features.SubmitBillingExportJob;

public sealed class SubmitBillingExportJobCommandValidator : AbstractValidator<SubmitBillingExportJobCommand>
{
    private static readonly Regex PayerPattern = new("^[A-Z0-9-]{2,16}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public SubmitBillingExportJobCommandValidator()
    {
        RuleFor(static c => c.PayerCode, nameof(SubmitBillingExportJobCommand.PayerCode))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && PayerPattern.IsMatch(v))
            .WithMessage("PayerCode must be 2–16 uppercase alphanumeric or hyphen characters.");

        RuleFor(static c => c.PeriodStart, nameof(SubmitBillingExportJobCommand.PeriodStart))
            .Must(static (cmd, v) => v < cmd.PeriodEnd)
            .WithMessage("PeriodStart must be earlier than PeriodEnd.");

        RuleFor(static c => c.Notes, nameof(SubmitBillingExportJobCommand.Notes))
            .Must(static (_, v) => v is null || v.Length <= 500)
            .WithMessage("Notes must be at most 500 characters.");
    }
}
