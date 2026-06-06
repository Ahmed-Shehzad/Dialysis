using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class CarePlanRepository : ICarePlanRepository
{
    private readonly EhrDbContext _db;
    public CarePlanRepository(EhrDbContext db) => _db = db;

    public Task<CarePlan?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.CarePlans.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CarePlan?> GetActiveByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        _db.CarePlans
            .AsNoTracking()
            .Where(c => c.PatientId == patientId && !c.IsDeleted && c.Status == CarePlanStatus.Active)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(CarePlan carePlan) => _db.CarePlans.Add(carePlan);
}

public sealed class AllergyRepository : IAllergyRepository
{
    private readonly EhrDbContext _db;
    public AllergyRepository(EhrDbContext db) => _db = db;
    public Task<Allergy?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Allergies.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Allergy>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.Allergies.Where(a => a.PatientId == patientId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(Allergy allergy) => _db.Allergies.Add(allergy);

    public IAsyncEnumerable<Allergy> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = _db.Allergies.AsNoTracking().OrderBy(a => a.UpdatedAtUtc).ThenBy(a => a.Id).AsQueryable();
        if (since is { } cutoff)
            query = query.Where(a => a.UpdatedAtUtc >= cutoff);
        return query.AsAsyncEnumerable();
    }
}

public sealed class ProblemListRepository : IProblemListRepository
{
    private readonly EhrDbContext _db;
    public ProblemListRepository(EhrDbContext db) => _db = db;
    public Task<ProblemListItem?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ProblemListItems.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProblemListItem>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var q = _db.ProblemListItems.Where(p => p.PatientId == patientId);
        if (activeOnly) q = q.Where(p => p.Status == ProblemStatus.Active);
        return await q.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Add(ProblemListItem item) => _db.ProblemListItems.Add(item);
}

public sealed class VitalSignRepository : IVitalSignRepository
{
    private readonly EhrDbContext _db;
    public VitalSignRepository(EhrDbContext db) => _db = db;
    public async Task<IReadOnlyList<VitalSignReading>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        await _db.VitalSignReadings
            .Where(v => v.PatientId == patientId && v.ObservedAtUtc >= sinceUtc)
            .OrderByDescending(v => v.ObservedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(VitalSignReading reading) => _db.VitalSignReadings.Add(reading);

    public IAsyncEnumerable<VitalSignReading> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = _db.VitalSignReadings.AsNoTracking().OrderBy(v => v.ObservedAtUtc).AsQueryable();
        if (since is { } cutoff)
        {
            var cutoffUtc = cutoff.UtcDateTime;
            query = query.Where(v => v.ObservedAtUtc >= cutoffUtc);
        }
        return query.AsAsyncEnumerable();
    }
}

public sealed class ImmunizationRepository : IImmunizationRepository
{
    private readonly EhrDbContext _db;
    public ImmunizationRepository(EhrDbContext db) => _db = db;
    public async Task<IReadOnlyList<Immunization>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.Immunizations
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.AdministeredOn)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(Immunization immunization) => _db.Immunizations.Add(immunization);

    public IAsyncEnumerable<Immunization> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = _db.Immunizations.AsNoTracking().OrderBy(i => i.UpdatedAtUtc).ThenBy(i => i.Id).AsQueryable();
        if (since is { } cutoff)
            query = query.Where(i => i.UpdatedAtUtc >= cutoff);
        return query.AsAsyncEnumerable();
    }
}

public sealed class MedicationStatementRepository : IMedicationStatementRepository
{
    private readonly EhrDbContext _db;
    public MedicationStatementRepository(EhrDbContext db) => _db = db;
    public Task<MedicationStatement?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.MedicationStatements.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MedicationStatement>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var q = _db.MedicationStatements.Where(m => m.PatientId == patientId);
        if (activeOnly) q = q.Where(m => m.Status == MedicationStatementStatus.Active);
        return await q.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Add(MedicationStatement statement) => _db.MedicationStatements.Add(statement);

    public IAsyncEnumerable<MedicationStatement> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = _db.MedicationStatements.AsNoTracking().OrderBy(m => m.UpdatedAtUtc).ThenBy(m => m.Id).AsQueryable();
        if (since is { } cutoff)
            query = query.Where(m => m.UpdatedAtUtc >= cutoff);
        return query.AsAsyncEnumerable();
    }
}
