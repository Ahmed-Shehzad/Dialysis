using Dialysis.Domain.Aggregates;
using Dialysis.Persistence;
using Dialysis.Persistence.Queries;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Observations.Get;

public sealed class GetObservationQueryHandler : IQueryHandler<GetObservationQuery, Observation?>
{
    private readonly DialysisDbContext _db;

    public GetObservationQueryHandler(DialysisDbContext db) => _db = db;

    public async Task<Observation?> HandleAsync(GetObservationQuery request, CancellationToken cancellationToken = default)
    {
        var tenantStr = request.TenantId.Value;
        var observationStr = request.ObservationId.Value;
        return await CompiledQueries.GetObservationById(_db, tenantStr, observationStr);
    }
}
