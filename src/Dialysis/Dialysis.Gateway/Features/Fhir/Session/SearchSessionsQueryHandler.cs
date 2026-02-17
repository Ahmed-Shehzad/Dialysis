using Dialysis.Persistence;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;
using SessionAggregate = Dialysis.Domain.Aggregates.Session;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.Fhir.Session;

public sealed class SearchSessionsQueryHandler : IQueryHandler<SearchSessionsQuery, IReadOnlyList<SessionAggregate>>
{
    private readonly DialysisDbContext _db;
    private readonly ITenantContext _tenantContext;

    public SearchSessionsQueryHandler(DialysisDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<SessionAggregate>> HandleAsync(SearchSessionsQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);

        var query = _db.Sessions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.PatientId == patientId)
            .OrderByDescending(s => s.StartedAt)
            .Skip(request.Offset)
            .Take(request.Limit ?? 100);
        return await query.ToListAsync(cancellationToken);
    }
}
