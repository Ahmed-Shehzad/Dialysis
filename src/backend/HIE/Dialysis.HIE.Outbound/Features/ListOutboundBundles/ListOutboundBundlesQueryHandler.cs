using Dialysis.CQRS.Queries;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Ports;

namespace Dialysis.HIE.Outbound.Features.ListOutboundBundles;

public sealed class ListOutboundBundlesQueryHandler : IQueryHandler<ListOutboundBundlesQuery, IReadOnlyList<OutboundBundleDto>>
{
    private readonly IOutboundBundleStore _store;
    public ListOutboundBundlesQueryHandler(IOutboundBundleStore store) => _store = store;
    public async Task<IReadOnlyList<OutboundBundleDto>> HandleAsync(
        ListOutboundBundlesQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        OutboundBundleStatus? statusFilter = request.StatusFilter.HasValue
            ? (OutboundBundleStatus)request.StatusFilter.Value
            : null;

        var rows = await _store.ListAsync(statusFilter, take, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new OutboundBundleDto(
            r.Id,
            r.PatientId,
            r.ResourceType,
            r.LogicalId,
            r.PartnerId,
            (int)r.Status,
            r.Attempts,
            r.CreatedAtUtc,
            r.NextAttemptAtUtc,
            r.DeliveredAtUtc,
            r.LastFailureReason))];
    }
}
