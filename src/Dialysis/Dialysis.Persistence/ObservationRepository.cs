using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class ObservationRepository : IObservationRepository
{
    private readonly DialysisDbContext _db;

    public ObservationRepository(DialysisDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        _db.Observations.Add(observation);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Observation?> GetByIdAsync(TenantId tenantId, ObservationId observationId, CancellationToken cancellationToken = default)
    {
        return await _db.Observations
            .FirstOrDefaultAsync(
                o => o.TenantId == tenantId && o.Id.ToString() == observationId.Value,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Observation>> GetByPatientAsync(TenantId tenantId, PatientId patientId, CancellationToken cancellationToken = default)
    {
        return await _db.Observations
            .Where(o => o.TenantId == tenantId && o.PatientId == patientId)
            .OrderByDescending(o => o.Effective.Value)
            .ToListAsync(cancellationToken);
    }
}
