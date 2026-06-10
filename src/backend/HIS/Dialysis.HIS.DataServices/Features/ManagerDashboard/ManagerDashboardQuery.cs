using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ManagerDashboard;

public sealed record ManagerDashboardQuery : IQuery<ManagerDashboardSnapshotDto>, IPermissionedCommand
{
    public ManagerDashboardQuery(string? ReportFocus = null) => this.ReportFocus = ReportFocus;
    public string RequiredPermission => HisPermissions.DataReport;
    public string? ReportFocus { get; init; }
    public void Deconstruct(out string? reportFocus) => reportFocus = ReportFocus;
}
