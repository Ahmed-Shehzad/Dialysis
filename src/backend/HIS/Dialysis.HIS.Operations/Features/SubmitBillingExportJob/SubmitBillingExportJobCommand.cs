using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.SubmitBillingExportJob;

public sealed record SubmitBillingExportJobCommand(
    string PayerCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? Notes = null) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DataReport;
}
