using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ManagerDashboard;

public sealed class ManagerDashboardQueryHandler(IManagerDashboardReadModel readModel)
    : IQueryHandler<ManagerDashboardQuery, ManagerDashboardSnapshotDto>
{
    public Task<ManagerDashboardSnapshotDto> Handle(ManagerDashboardQuery request, CancellationToken cancellationToken)
    {
        var focus = string.IsNullOrWhiteSpace(request.ReportFocus) ? null : request.ReportFocus.Trim();
        return readModel.SnapshotAsync(focus, cancellationToken);
    }
}
