using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ManagerDashboard;

/// <param name="ReportFocus">Optional label echoed in <see cref="ManagerDashboardDto.AppliedReportFocus"/> for scheduled-report or BI routing hooks.</param>
public sealed record ManagerDashboardQuery(string? ReportFocus = null)
    : IQuery<ManagerDashboardDto>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DataReport;
}
