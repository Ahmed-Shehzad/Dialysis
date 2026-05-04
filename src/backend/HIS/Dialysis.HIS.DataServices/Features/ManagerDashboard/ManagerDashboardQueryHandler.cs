using Dialysis.CQRS.Queries;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ManagerDashboard;

public sealed class ManagerDashboardQueryHandler(IManagerDashboardReadModel readModel)
    : IQueryHandler<ManagerDashboardQuery, ManagerDashboardDto>
{
    public Task<ManagerDashboardDto> Handle(ManagerDashboardQuery request, CancellationToken cancellationToken) =>
        readModel.GetAsync(request.ReportFocus, cancellationToken);
}
