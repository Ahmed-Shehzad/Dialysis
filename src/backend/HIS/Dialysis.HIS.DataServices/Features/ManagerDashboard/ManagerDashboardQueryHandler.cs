using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ManagerDashboard;

public sealed class ManagerDashboardQueryHandler : IQueryHandler<ManagerDashboardQuery, ManagerDashboardSnapshotDto>
{
    private readonly IManagerDashboardReadModel _readModel;
    public ManagerDashboardQueryHandler(IManagerDashboardReadModel readModel) => _readModel = readModel;
    public Task<ManagerDashboardSnapshotDto> HandleAsync(ManagerDashboardQuery request, CancellationToken cancellationToken)
    {
        var focus = string.IsNullOrWhiteSpace(request.ReportFocus) ? null : request.ReportFocus.Trim();
        return _readModel.SnapshotAsync(focus, cancellationToken);
    }
}
