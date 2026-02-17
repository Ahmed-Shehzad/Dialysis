using ConditionEntity = Dialysis.Domain.Entities.Condition;
using Dialysis.Persistence;
using Dialysis.Persistence.Queries;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.Condition;

public sealed class GetConditionQueryHandler : IQueryHandler<GetConditionQuery, ConditionEntity?>
{
    private readonly DialysisDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetConditionQueryHandler(DialysisDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<ConditionEntity?> HandleAsync(GetConditionQuery request, CancellationToken cancellationToken = default)
    {
        if (!Ulid.TryParse(request.Id, out var ulid))
            return null;

        var tenantStr = _tenantContext.TenantId.Value;
        return await CompiledQueries.GetConditionById(_db, tenantStr, ulid);
    }
}
