using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.IntegrationEvents;

namespace Dialysis.HIS.PatientFlow.Domain;

public sealed class Patient : AggregateRoot<Guid>
{
    public Patient()
    {
    }

    public Patient(Guid id)
        : base(id)
    {
    }

    public string MedicalRecordNumber { get; private set; } = string.Empty;

    public PatientVisitState VisitState { get; private set; }

    public DateTime? AdmittedAtUtc { get; private set; }

    public DateTime? DischargedAtUtc { get; private set; }

    public void RegisterNewPatient(string medicalRecordNumber, DateTime utcNow, string? actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(medicalRecordNumber);
        if (VisitState != PatientVisitState.NotAdmitted)
            throw new InvalidOperationException("Patient is already registered.");

        MedicalRecordNumber = medicalRecordNumber;
        RecordCreation(utcNow, actorId);
    }

    public void Admit(DateTime utcNow, string? actorId)
    {
        if (VisitState == PatientVisitState.InHouse)
            throw new InvalidOperationException("Patient is already admitted.");

        if (VisitState == PatientVisitState.Discharged)
            throw new InvalidOperationException("Cannot admit a discharged patient without a new encounter.");

        VisitState = PatientVisitState.InHouse;
        AdmittedAtUtc = utcNow;
        RecordUpdate(utcNow, actorId);
        RaiseIntegrationEvent(new PatientAdmittedToHospitalIntegrationEvent(Id, MedicalRecordNumber, utcNow));
    }

    public void Discharge(DateTime utcNow, string? actorId)
    {
        if (VisitState != PatientVisitState.InHouse)
            throw new InvalidOperationException("Patient must be in-house to discharge.");

        VisitState = PatientVisitState.Discharged;
        DischargedAtUtc = utcNow;
        RecordUpdate(utcNow, actorId);
        RaiseIntegrationEvent(new PatientDischargedIntegrationEvent(Id, utcNow));
    }
}
