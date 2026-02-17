using Dialysis.Domain.Aggregates;
using Dialysis.Persistence;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.DeviceIngestion.Features.Observations.Search;

public sealed class SearchObservationsQueryHandler : IQueryHandler<SearchObservationsQuery, IReadOnlyList<Observation>>
{
    private readonly DialysisDbContext _db;

    public SearchObservationsQueryHandler(DialysisDbContext db) => _db = db;

    public async Task<IReadOnlyList<Observation>> HandleAsync(SearchObservationsQuery request, CancellationToken cancellationToken = default)
    {
        return await _db.Observations
            .AsNoTracking()
            .Where(o => o.TenantId == request.TenantId && o.PatientId == request.PatientId)
            .OrderByDescending(o => o.Effective.Value)
            .ToListAsync(cancellationToken);
    }
}
