using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents;

public sealed record PatientAdmittedToHospitalIntegrationEvent(
    Guid PatientId,
    string MedicalRecordNumber,
    DateTime AdmittedAtUtc)
    : IntegrationEvent;
