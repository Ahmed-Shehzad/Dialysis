namespace Dialysis.HIS.Contracts.IntegrationEvents;

/// <summary>
/// Stable names for integration event types (versioning / routing).
/// </summary>
public static class IntegrationEventCatalog
{
    public const string PatientAdmittedV1 = nameof(PatientAdmittedToHospitalIntegrationEvent);
    public const string PatientDischargedV1 = nameof(PatientDischargedIntegrationEvent);
    public const string ReferralCreatedV1 = nameof(ReferralCreatedIntegrationEvent);
    public const string AppointmentBookedV1 = nameof(AppointmentBookedIntegrationEvent);
    public const string MedicationOrderPlacedV1 = nameof(MedicationOrderPlacedIntegrationEvent);
    public const string MedicationOrderDiscontinuedV1 = nameof(MedicationOrderDiscontinuedIntegrationEvent);
}
