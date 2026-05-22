using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class EncounterRepository(EhrDbContext db) : IEncounterRepository
{
    public Task<Encounter?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Encounters.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(Encounter encounter) => db.Encounters.Add(encounter);
}

public sealed class ClinicalNoteRepository(EhrDbContext db) : IClinicalNoteRepository
{
    public Task<ClinicalNote?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ClinicalNotes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ClinicalNote>> ListByEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default) =>
        await db.ClinicalNotes.Where(n => n.EncounterId == encounterId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ClinicalNote>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
        await db.ClinicalNotes
            .AsNoTracking()
            .Where(n => n.PatientId == patientId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(ClinicalNote note) => db.ClinicalNotes.Add(note);
}

public sealed class PrescriptionRepository(EhrDbContext db) : IPrescriptionRepository
{
    public Task<Prescription?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Prescriptions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Prescription prescription) => db.Prescriptions.Add(prescription);
}

public sealed class LabOrderRepository(EhrDbContext db) : ILabOrderRepository
{
    public Task<LabOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.LabOrders.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public void Add(LabOrder labOrder) => db.LabOrders.Add(labOrder);
}

public sealed class LabResultRepository(EhrDbContext db) : ILabResultRepository
{
    public async Task<IReadOnlyList<LabResult>> ListByOrderAsync(Guid labOrderId, CancellationToken cancellationToken = default) =>
        await db.LabResults.Where(r => r.LabOrderId == labOrderId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<LabResult>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        await db.LabResults
            .Where(r => r.PatientId == patientId && r.ObservedAtUtc >= sinceUtc)
            .OrderByDescending(r => r.ObservedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(LabResult result) => db.LabResults.Add(result);
}
