using Dialysis.CQRS.Queries;
using Dialysis.HIS.Scheduling.Ports;

namespace Dialysis.HIS.Scheduling.Features.ListSchedulingResources;

public sealed class ListSchedulingResourcesQueryHandler(ISchedulingResourceDirectory directory)
    : IQueryHandler<ListSchedulingResourcesQuery, IReadOnlyList<SchedulingResourceDto>>
{
    public async Task<IReadOnlyList<SchedulingResourceDto>> Handle(
        ListSchedulingResourcesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await directory.ListAsync(request.KindCode, cancellationToken).ConfigureAwait(false);
        return rows.Select(r => new SchedulingResourceDto(r.Id, r.KindCode, r.DisplayName, r.IsBookable)).ToList();
    }
}
