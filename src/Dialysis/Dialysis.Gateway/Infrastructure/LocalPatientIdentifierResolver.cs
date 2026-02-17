using Dialysis.Persistence;
using Dialysis.Persistence.Queries;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Local MPI: looks up patients by LogicalId (MRN) in the PDMS database. Phase 4.2.1.
/// </summary>
public sealed class LocalPatientIdentifierResolver : IPatientIdentifierResolver
{
    private readonly DialysisDbContext _db;

    public LocalPatientIdentifierResolver(DialysisDbContext db) => _db = db;

    public async Task<PatientId?> ResolveByMrnAsync(TenantId tenantId, string mrn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mrn))
            return null;

        var patientId = new PatientId(mrn.Trim());
        var tenantStr = tenantId.Value;
        var mrnStr = patientId.Value;
        var exists = await CompiledQueries.PatientExists(_db, tenantStr, mrnStr);
        return exists ? patientId : null;
    }
}
