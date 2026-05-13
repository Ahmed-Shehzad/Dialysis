using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Contracts.Security;

/// <summary>
/// Closed permission set for the EHR module. Mirrors the practice-workflow surface:
/// Front Office (Registration / Scheduling / Patient Chart), Patient Portal, Physician (Clinical Notes),
/// Billing, plus integration with Pharmacy / Lab / Insurer.
/// </summary>
public static class EhrPermissions
{
    // Registration & demographics
    public const string PatientRegister = "ehr.registration.patient.register";
    public const string PatientUpdate = "ehr.registration.patient.update";
    public const string PatientMerge = "ehr.registration.patient.merge";
    public const string PatientRead = "ehr.registration.patient.read";

    // Patient chart (longitudinal record)
    public const string ChartRead = "ehr.patientchart.read";
    public const string AllergyRecord = "ehr.patientchart.allergy.record";
    public const string ProblemRecord = "ehr.patientchart.problem.record";
    public const string VitalsRecord = "ehr.patientchart.vitals.record";
    public const string ImmunizationRecord = "ehr.patientchart.immunization.record";
    public const string MedicationRecord = "ehr.patientchart.medication.record";

    // Scheduling
    public const string AppointmentBook = "ehr.scheduling.appointment.book";
    public const string AppointmentCancel = "ehr.scheduling.appointment.cancel";
    public const string AppointmentReschedule = "ehr.scheduling.appointment.reschedule";
    public const string AppointmentCheckIn = "ehr.scheduling.appointment.check-in";
    public const string ScheduleRead = "ehr.scheduling.read";

    // Patient portal
    public const string PortalRead = "ehr.portal.read";
    public const string PortalAppointmentRequest = "ehr.portal.appointment.request";
    public const string PortalMessageSend = "ehr.portal.message.send";

    // Clinical notes / encounters
    public const string EncounterStart = "ehr.clinical.encounter.start";
    public const string EncounterClose = "ehr.clinical.encounter.close";
    public const string ClinicalNoteWrite = "ehr.clinical.note.write";
    public const string ClinicalNoteSign = "ehr.clinical.note.sign";
    public const string ClinicalNoteRead = "ehr.clinical.note.read";
    public const string DiagnosisAttach = "ehr.clinical.diagnosis.attach";

    // Prescriptions
    public const string PrescriptionOrder = "ehr.prescription.order";
    public const string PrescriptionCancel = "ehr.prescription.cancel";

    // Lab
    public const string LabOrder = "ehr.lab.order";
    public const string LabResultRead = "ehr.lab.result.read";

    // Billing
    public const string ChargeCapture = "ehr.billing.charge.capture";
    public const string ClaimSubmit = "ehr.billing.claim.submit";
    public const string PaymentPost = "ehr.billing.payment.post";
    public const string StatementRead = "ehr.billing.statement.read";

    // Integration (Pharmacy / Lab / Insurer) admin surfaces
    public const string IntegrationOutboundManage = "ehr.integration.outbound.manage";
    public const string IntegrationInboundIngest = "ehr.integration.inbound.ingest";

    public static IReadOnlyList<string> All { get; } =
    [
        PatientRegister, PatientUpdate, PatientMerge, PatientRead,
        ChartRead, AllergyRecord, ProblemRecord, VitalsRecord, ImmunizationRecord, MedicationRecord,
        AppointmentBook, AppointmentCancel, AppointmentReschedule, AppointmentCheckIn, ScheduleRead,
        PortalRead, PortalAppointmentRequest, PortalMessageSend,
        EncounterStart, EncounterClose, ClinicalNoteWrite, ClinicalNoteSign, ClinicalNoteRead, DiagnosisAttach,
        PrescriptionOrder, PrescriptionCancel,
        LabOrder, LabResultRead,
        ChargeCapture, ClaimSubmit, PaymentPost, StatementRead,
        IntegrationOutboundManage, IntegrationInboundIngest,
    ];
}

/// <summary>Module-hosting catalog binding for <see cref="EhrPermissions"/>.</summary>
public sealed class EhrPermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "ehr";

    public IReadOnlyCollection<string> All => EhrPermissions.All;
}
