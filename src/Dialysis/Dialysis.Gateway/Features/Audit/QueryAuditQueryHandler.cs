using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Audit;

public sealed class QueryAuditQueryHandler : IQueryHandler<QueryAuditQuery, IReadOnlyList<AuditEventDto>>
{
    private readonly IAuditRepository _repository;
    private readonly ITenantContext _tenantContext;

    public QueryAuditQueryHandler(IAuditRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<AuditEventDto>> HandleAsync(QueryAuditQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var events = await _repository.QueryAsync(
            tenantId,
            request.PatientId,
            request.ResourceType,
            request.Action,
            request.From,
            request.To,
            request.Limit,
            request.Offset,
            cancellationToken);

        return events.Select(ToDto).ToList();
    }

    private static AuditEventDto ToDto(AuditEvent e) => new(
        e.Id.ToString(),
        e.Actor,
        e.Action,
        e.ResourceType,
        e.ResourceId,
        e.PatientId,
        e.CreatedAtUtc,
        e.Details);
}
