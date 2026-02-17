using Dialysis.Persistence;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.Fhir.Condition;

public sealed class SearchConditionsQueryHandler : IQueryHandler<SearchConditionsQuery, IReadOnlyList<Dialysis.Domain.Entities.Condition>>
{
    private readonly DialysisDbContext _db;
    private readonly ITenantContext _tenantContext;

    public SearchConditionsQueryHandler(DialysisDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<Dialysis.Domain.Entities.Condition>> HandleAsync(SearchConditionsQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);

        return await _db.Conditions
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.PatientId == patientId)
            .OrderByDescending(c => c.RecordedDate)
            .ToListAsync(cancellationToken);
    }
}
