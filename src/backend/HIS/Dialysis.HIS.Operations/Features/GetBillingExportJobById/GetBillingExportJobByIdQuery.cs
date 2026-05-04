using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.GetBillingExportJobById;

public sealed record GetBillingExportJobByIdQuery(Guid Id)
    : IQuery<BillingExportJobStatusDto?>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.BillingExport;
}

public sealed record BillingExportJobStatusDto(
    Guid Id,
    DateTime RequestedAtUtc,
    string FormatCode,
    string StatusCode,
    string? PayerCode);
