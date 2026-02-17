using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Hl7;

public sealed class ListFailedHl7QueryHandler : IQueryHandler<ListFailedHl7Query, IReadOnlyList<FailedHl7MessageDto>>
{
    private readonly IFailedHl7MessageStore _store;
    private readonly ITenantContext _tenantContext;

    public ListFailedHl7QueryHandler(IFailedHl7MessageStore store, ITenantContext tenantContext)
    {
        _store = store;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<FailedHl7MessageDto>> HandleAsync(ListFailedHl7Query request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var items = await _store.ListAsync(tenantId, request.Limit, request.Offset, cancellationToken);
        return items.Select(f => new FailedHl7MessageDto(f.Id.ToString(), f.MessageControlId, f.ErrorMessage, f.FailedAtUtc, f.RetryCount)).ToList();
    }
}
