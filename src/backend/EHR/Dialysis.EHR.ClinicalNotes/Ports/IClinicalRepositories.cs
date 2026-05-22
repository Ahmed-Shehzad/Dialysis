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

    /// <summary>
    /// Lists the most-recent <see cref="ClinicalNote"/> records authored for a patient,
    /// ordered by created-at descending. Drives the chart's Notes section so a clinician
    /// can see what's been written across encounters without drilling into each one.
    /// </summary>
    Task<IReadOnlyList<ClinicalNote>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default);

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
