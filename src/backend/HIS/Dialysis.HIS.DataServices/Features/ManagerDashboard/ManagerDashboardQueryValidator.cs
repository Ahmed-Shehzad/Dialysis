using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.DataServices.Features.ManagerDashboard;

public sealed class ManagerDashboardQueryValidator : AbstractValidator<ManagerDashboardQuery>
{
    public ManagerDashboardQueryValidator() =>
        RuleFor(static c => c, ValidationPath.Root)
            .Must(static (_, q) => q.ReportFocus is null || q.ReportFocus.Trim().Length <= 128)
            .WithMessage($"{nameof(ManagerDashboardQuery.ReportFocus)} must be at most 128 characters.");
}
