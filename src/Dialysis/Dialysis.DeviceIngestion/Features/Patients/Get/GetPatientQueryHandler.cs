using Dialysis.Domain.Entities;
using Dialysis.Persistence;
using Dialysis.Persistence.Queries;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Patients.Get;

public sealed class GetPatientQueryHandler : IQueryHandler<GetPatientQuery, Patient?>
{
    private readonly DialysisDbContext _db;

    public GetPatientQueryHandler(DialysisDbContext db) => _db = db;

    public async Task<Patient?> HandleAsync(GetPatientQuery request, CancellationToken cancellationToken = default)
    {
        var tenantStr = request.TenantId.Value;
        var logicalIdStr = request.LogicalId.Value;
        return await CompiledQueries.GetPatientById(_db, tenantStr, logicalIdStr);
    }
}
