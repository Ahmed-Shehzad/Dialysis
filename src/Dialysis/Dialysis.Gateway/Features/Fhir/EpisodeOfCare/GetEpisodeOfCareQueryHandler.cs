using Dialysis.Persistence;
using Dialysis.Persistence.Queries;
using Dialysis.SharedKernel.Abstractions;
using EpisodeOfCareEntity = Dialysis.Domain.Entities.EpisodeOfCare;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.EpisodeOfCare;

public sealed class GetEpisodeOfCareQueryHandler : IQueryHandler<GetEpisodeOfCareQuery, EpisodeOfCareEntity?>
{
    private readonly DialysisDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetEpisodeOfCareQueryHandler(DialysisDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<EpisodeOfCareEntity?> HandleAsync(GetEpisodeOfCareQuery request, CancellationToken cancellationToken = default)
    {
        if (!Ulid.TryParse(request.Id, out var ulid))
            return null;

        var tenantStr = _tenantContext.TenantId.Value;
        return await CompiledQueries.GetEpisodeOfCareById(_db, tenantStr, ulid);
    }
}
