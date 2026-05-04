using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.CreateBillingExportJob;

public sealed record CreateBillingExportJobCommand(string FormatCode, string? PayerCode = null)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.BillingExport;
}
