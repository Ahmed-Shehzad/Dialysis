using EpisodeOfCareEntity = Dialysis.Domain.Entities.EpisodeOfCare;
using Dialysis.Persistence;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.Fhir.EpisodeOfCare;

public sealed class SearchEpisodesOfCareQueryHandler : IQueryHandler<SearchEpisodesOfCareQuery, IReadOnlyList<EpisodeOfCareEntity>>
{
    private readonly DialysisDbContext _db;
    private readonly ITenantContext _tenantContext;

    public SearchEpisodesOfCareQueryHandler(DialysisDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<EpisodeOfCareEntity>> HandleAsync(SearchEpisodesOfCareQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);

        return await _db.EpisodeOfCare
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.PatientId == patientId)
            .OrderByDescending(e => e.PeriodStart)
            .ToListAsync(cancellationToken);
    }
}
