using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class AllergyRepository(EhrDbContext db) : IAllergyRepository
{
    public Task<Allergy?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Allergies.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Allergy>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await db.Allergies.Where(a => a.PatientId == patientId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(Allergy allergy) => db.Allergies.Add(allergy);

    public IAsyncEnumerable<Allergy> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = db.Allergies.AsNoTracking().OrderBy(a => a.UpdatedAtUtc).ThenBy(a => a.Id).AsQueryable();
        if (since is { } cutoff)
            query = query.Where(a => a.UpdatedAtUtc >= cutoff);
        return query.AsAsyncEnumerable();
    }
}

public sealed class ProblemListRepository(EhrDbContext db) : IProblemListRepository
{
    public Task<ProblemListItem?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ProblemListItems.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProblemListItem>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var q = db.ProblemListItems.Where(p => p.PatientId == patientId);
        if (activeOnly) q = q.Where(p => p.Status == ProblemStatus.Active);
        return await q.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Add(ProblemListItem item) => db.ProblemListItems.Add(item);
}

public sealed class VitalSignRepository(EhrDbContext db) : IVitalSignRepository
{
    public async Task<IReadOnlyList<VitalSignReading>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        await db.VitalSignReadings
            .Where(v => v.PatientId == patientId && v.ObservedAtUtc >= sinceUtc)
            .OrderByDescending(v => v.ObservedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(VitalSignReading reading) => db.VitalSignReadings.Add(reading);

    public IAsyncEnumerable<VitalSignReading> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = db.VitalSignReadings.AsNoTracking().OrderBy(v => v.ObservedAtUtc).AsQueryable();
        if (since is { } cutoff)
        {
            var cutoffUtc = cutoff.UtcDateTime;
            query = query.Where(v => v.ObservedAtUtc >= cutoffUtc);
        }
        return query.AsAsyncEnumerable();
    }
}

public sealed class ImmunizationRepository(EhrDbContext db) : IImmunizationRepository
{
    public async Task<IReadOnlyList<Immunization>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await db.Immunizations
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.AdministeredOn)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(Immunization immunization) => db.Immunizations.Add(immunization);

    public IAsyncEnumerable<Immunization> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = db.Immunizations.AsNoTracking().OrderBy(i => i.UpdatedAtUtc).ThenBy(i => i.Id).AsQueryable();
        if (since is { } cutoff)
            query = query.Where(i => i.UpdatedAtUtc >= cutoff);
        return query.AsAsyncEnumerable();
    }
}

public sealed class MedicationStatementRepository(EhrDbContext db) : IMedicationStatementRepository
{
    public Task<MedicationStatement?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.MedicationStatements.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MedicationStatement>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var q = db.MedicationStatements.Where(m => m.PatientId == patientId);
        if (activeOnly) q = q.Where(m => m.Status == MedicationStatementStatus.Active);
        return await q.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Add(MedicationStatement statement) => db.MedicationStatements.Add(statement);

    public IAsyncEnumerable<MedicationStatement> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = db.MedicationStatements.AsNoTracking().OrderBy(m => m.UpdatedAtUtc).ThenBy(m => m.Id).AsQueryable();
        if (since is { } cutoff)
            query = query.Where(m => m.UpdatedAtUtc >= cutoff);
        return query.AsAsyncEnumerable();
    }
}
