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

    /// <summary>
    /// Lists a patient's <see cref="PrescriptionStatus.Active"/> prescriptions. Drives the
    /// point-of-care duplicate-medication safety check at prescribe time.
    /// </summary>
    Task<IReadOnlyList<Prescription>> ListActiveByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    void Add(Prescription prescription);
}

public interface ILabOrderRepository
{
    Task<LabOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists a patient's non-cancelled lab orders created on or after <paramref name="sinceUtc"/>.
    /// Drives the point-of-care duplicate-lab-order safety check at order time.
    /// </summary>
    Task<IReadOnlyList<LabOrder>> ListRecentByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default);

    void Add(LabOrder labOrder);
}

public interface IImagingOrderRepository
{
    Task<ImagingOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ImagingOrder?> GetByAccessionNumberAsync(string accessionNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImagingOrder>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default);
    void Add(ImagingOrder imagingOrder);
}

public interface ILabResultRepository
{
    Task<IReadOnlyList<LabResult>> ListByOrderAsync(Guid labOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabResult>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default);
    void Add(LabResult result);
}
