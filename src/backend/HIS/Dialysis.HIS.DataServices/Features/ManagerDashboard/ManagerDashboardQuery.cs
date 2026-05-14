using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ManagerDashboard;

public sealed record ManagerDashboardQuery(string? ReportFocus = null)
    : IQuery<ManagerDashboardSnapshotDto>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DataReport;
}
