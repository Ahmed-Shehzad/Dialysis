using Dialysis.Persistence;
using Dialysis.Persistence.Queries;
using Dialysis.SharedKernel.Abstractions;
using SessionAggregate = Dialysis.Domain.Aggregates.Session;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.Session;

public sealed class GetSessionQueryHandler : IQueryHandler<GetSessionQuery, SessionAggregate?>
{
    private readonly DialysisDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetSessionQueryHandler(DialysisDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<SessionAggregate?> HandleAsync(GetSessionQuery request, CancellationToken cancellationToken = default)
    {
        var tenantStr = _tenantContext.TenantId.Value;
        return await CompiledQueries.GetSessionById(_db, tenantStr, request.SessionId);
    }
}
