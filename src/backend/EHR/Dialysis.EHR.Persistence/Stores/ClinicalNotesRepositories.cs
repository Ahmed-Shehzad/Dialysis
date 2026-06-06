using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class EncounterRepository : IEncounterRepository
{
    private readonly EhrDbContext _db;
    public EncounterRepository(EhrDbContext db) => _db = db;
    public Task<Encounter?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Encounters.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(Encounter encounter) => _db.Encounters.Add(encounter);
}

public sealed class ClinicalNoteRepository : IClinicalNoteRepository
{
    private readonly EhrDbContext _db;
    public ClinicalNoteRepository(EhrDbContext db) => _db = db;
    public Task<ClinicalNote?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ClinicalNotes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ClinicalNote>> ListByEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default) =>
        await _db.ClinicalNotes.Where(n => n.EncounterId == encounterId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ClinicalNote>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
        await _db.ClinicalNotes
            .AsNoTracking()
            .Where(n => n.PatientId == patientId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(ClinicalNote note) => _db.ClinicalNotes.Add(note);
}

public sealed class PrescriptionRepository : IPrescriptionRepository
{
    private readonly EhrDbContext _db;
    public PrescriptionRepository(EhrDbContext db) => _db = db;
    public Task<Prescription?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Prescriptions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Prescription>> ListActiveByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.PatientId == patientId && !p.IsDeleted && p.Status == PrescriptionStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(Prescription prescription) => _db.Prescriptions.Add(prescription);
}

public sealed class LabOrderRepository : ILabOrderRepository
{
    private readonly EhrDbContext _db;
    public LabOrderRepository(EhrDbContext db) => _db = db;
    public Task<LabOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.LabOrders.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LabOrder>> ListRecentByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        await _db.LabOrders
            .AsNoTracking()
            .Where(l => l.PatientId == patientId && !l.IsDeleted
                && l.Status != LabOrderStatus.Cancelled && l.CreatedAt >= sinceUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(LabOrder labOrder) => _db.LabOrders.Add(labOrder);
}

public sealed class ImagingOrderRepository : IImagingOrderRepository
{
    private readonly EhrDbContext _db;
    public ImagingOrderRepository(EhrDbContext db) => _db = db;

    public Task<ImagingOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ImagingOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<ImagingOrder?> GetByAccessionNumberAsync(string accessionNumber, CancellationToken cancellationToken = default) =>
        _db.ImagingOrders.FirstOrDefaultAsync(o => o.AccessionNumber == accessionNumber, cancellationToken);

    public async Task<IReadOnlyList<ImagingOrder>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
        await _db.ImagingOrders
            .AsNoTracking()
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.Id)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(ImagingOrder imagingOrder) => _db.ImagingOrders.Add(imagingOrder);
}

public sealed class LabResultRepository : ILabResultRepository
{
    private readonly EhrDbContext _db;
    public LabResultRepository(EhrDbContext db) => _db = db;
    public async Task<IReadOnlyList<LabResult>> ListByOrderAsync(Guid labOrderId, CancellationToken cancellationToken = default) =>
        await _db.LabResults.Where(r => r.LabOrderId == labOrderId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<LabResult>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        await _db.LabResults
            .Where(r => r.PatientId == patientId && r.ObservedAtUtc >= sinceUtc)
            .OrderByDescending(r => r.ObservedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(LabResult result) => _db.LabResults.Add(result);
}
