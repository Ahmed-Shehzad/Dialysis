using Dialysis.CQRS.Queries;
using Dialysis.HIE.Inbound.Ports;

namespace Dialysis.HIE.Inbound.Features.ListInboundResources;

public sealed class ListInboundResourcesQueryHandler : IQueryHandler<ListInboundResourcesQuery, IReadOnlyList<InboundResourceDto>>
{
    private readonly IReceivedResourceStore _store;
    public ListInboundResourcesQueryHandler(IReceivedResourceStore store) => _store = store;
    public async Task<IReadOnlyList<InboundResourceDto>> HandleAsync(
        ListInboundResourcesQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        var rows = await _store.ListRecentAsync(request.PartnerId, take, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new InboundResourceDto(
            r.Id,
            r.PartnerId,
            r.ResourceType,
            r.LogicalId,
            r.ReceivedAtUtc,
            r.ValidationOutcome))];
    }
}
