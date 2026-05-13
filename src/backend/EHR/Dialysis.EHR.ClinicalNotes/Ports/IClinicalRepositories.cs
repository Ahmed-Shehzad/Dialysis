using Dialysis.EHR.ClinicalNotes.Domain;

namespace Dialysis.EHR.ClinicalNotes.Ports;

public interface IEncounterRepository
{
    Task<Encounter?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Encounter encounter);
}

public interface IClinicalNoteRepository
{
    Task<ClinicalNote?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicalNote>> ListByEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default);
    void Add(ClinicalNote note);
}

public interface IPrescriptionRepository
{
    Task<Prescription?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Prescription prescription);
}

public interface ILabOrderRepository
{
    Task<LabOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(LabOrder labOrder);
}

public interface ILabResultRepository
{
    Task<IReadOnlyList<LabResult>> ListByOrderAsync(Guid labOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabResult>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default);
    void Add(LabResult result);
}
