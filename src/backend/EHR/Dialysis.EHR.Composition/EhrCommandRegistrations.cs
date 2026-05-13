using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.Billing.Features.CaptureCharge;
using Dialysis.EHR.Billing.Features.PostPayment;
using Dialysis.EHR.Billing.Features.RecordRemittance;
using Dialysis.EHR.Billing.Features.SubmitClaim;
using Dialysis.EHR.ClinicalNotes.Features.AttachDiagnosis;
using Dialysis.EHR.ClinicalNotes.Features.CloseEncounter;
using Dialysis.EHR.ClinicalNotes.Features.DraftClinicalNote;
using Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;
using Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;
using Dialysis.EHR.ClinicalNotes.Features.SignClinicalNote;
using Dialysis.EHR.ClinicalNotes.Features.StartEncounter;
using Dialysis.EHR.Integration.Features.IngestLabResult;
using Dialysis.EHR.PatientChart.Features.RecordAllergy;
using Dialysis.EHR.PatientChart.Features.RecordImmunization;
using Dialysis.EHR.PatientChart.Features.RecordMedicationStatement;
using Dialysis.EHR.PatientChart.Features.RecordProblem;
using Dialysis.EHR.PatientChart.Features.RecordVitalSign;
using Dialysis.EHR.PatientPortal.Features.RequestAppointment;
using Dialysis.EHR.PatientPortal.Features.SendSecureMessage;
using Dialysis.EHR.Registration.Features.MergePatients;
using Dialysis.EHR.Registration.Features.RegisterPatient;
using Dialysis.EHR.Registration.Features.RegisterProvider;
using Dialysis.EHR.Registration.Features.UpdatePatientDemographics;
using Dialysis.EHR.Scheduling.Features.BookAppointment;
using Dialysis.EHR.Scheduling.Features.CancelAppointment;
using Dialysis.EHR.Scheduling.Features.CheckInPatient;
using Dialysis.EHR.Scheduling.Features.RescheduleAppointment;
using Dialysis.Module.Hosting.Pipeline;

namespace Dialysis.EHR.Composition;

/// <summary>
/// Wires the shared <see cref="AuthorizationPipelineBehavior{TRequest,TResponse}"/> onto every
/// permissioned command/query in the EHR module.
/// </summary>
internal static class EhrCommandRegistrations
{
    public static void RegisterAuthorizationBehaviors(CqrsBuilder c)
    {
        // Registration
        c.AddCommandBehavior<RegisterPatientCommand, Guid, AuthorizationPipelineBehavior<RegisterPatientCommand, Guid>>();
        c.AddCommandBehavior<UpdatePatientDemographicsCommand, Unit, AuthorizationPipelineBehavior<UpdatePatientDemographicsCommand, Unit>>();
        c.AddCommandBehavior<RegisterProviderCommand, Guid, AuthorizationPipelineBehavior<RegisterProviderCommand, Guid>>();
        c.AddCommandBehavior<MergePatientsCommand, Unit, AuthorizationPipelineBehavior<MergePatientsCommand, Unit>>();

        // PatientChart
        c.AddCommandBehavior<RecordAllergyCommand, Guid, AuthorizationPipelineBehavior<RecordAllergyCommand, Guid>>();
        c.AddCommandBehavior<RecordProblemCommand, Guid, AuthorizationPipelineBehavior<RecordProblemCommand, Guid>>();
        c.AddCommandBehavior<RecordVitalSignCommand, Guid, AuthorizationPipelineBehavior<RecordVitalSignCommand, Guid>>();
        c.AddCommandBehavior<RecordImmunizationCommand, Guid, AuthorizationPipelineBehavior<RecordImmunizationCommand, Guid>>();
        c.AddCommandBehavior<RecordMedicationStatementCommand, Guid, AuthorizationPipelineBehavior<RecordMedicationStatementCommand, Guid>>();

        // Scheduling
        c.AddCommandBehavior<BookAppointmentCommand, Guid, AuthorizationPipelineBehavior<BookAppointmentCommand, Guid>>();
        c.AddCommandBehavior<CancelAppointmentCommand, Unit, AuthorizationPipelineBehavior<CancelAppointmentCommand, Unit>>();
        c.AddCommandBehavior<RescheduleAppointmentCommand, Unit, AuthorizationPipelineBehavior<RescheduleAppointmentCommand, Unit>>();
        c.AddCommandBehavior<CheckInPatientCommand, Unit, AuthorizationPipelineBehavior<CheckInPatientCommand, Unit>>();

        // Portal
        c.AddCommandBehavior<RequestAppointmentCommand, Guid, AuthorizationPipelineBehavior<RequestAppointmentCommand, Guid>>();
        c.AddCommandBehavior<SendSecureMessageCommand, Guid, AuthorizationPipelineBehavior<SendSecureMessageCommand, Guid>>();

        // ClinicalNotes
        c.AddCommandBehavior<StartEncounterCommand, Guid, AuthorizationPipelineBehavior<StartEncounterCommand, Guid>>();
        c.AddCommandBehavior<AttachDiagnosisCommand, Unit, AuthorizationPipelineBehavior<AttachDiagnosisCommand, Unit>>();
        c.AddCommandBehavior<CloseEncounterCommand, Unit, AuthorizationPipelineBehavior<CloseEncounterCommand, Unit>>();
        c.AddCommandBehavior<DraftClinicalNoteCommand, Guid, AuthorizationPipelineBehavior<DraftClinicalNoteCommand, Guid>>();
        c.AddCommandBehavior<SignClinicalNoteCommand, Unit, AuthorizationPipelineBehavior<SignClinicalNoteCommand, Unit>>();
        c.AddCommandBehavior<OrderPrescriptionCommand, Guid, AuthorizationPipelineBehavior<OrderPrescriptionCommand, Guid>>();
        c.AddCommandBehavior<OrderLabTestCommand, Guid, AuthorizationPipelineBehavior<OrderLabTestCommand, Guid>>();

        // Billing
        c.AddCommandBehavior<CaptureChargeCommand, Guid, AuthorizationPipelineBehavior<CaptureChargeCommand, Guid>>();
        c.AddCommandBehavior<SubmitClaimCommand, Guid, AuthorizationPipelineBehavior<SubmitClaimCommand, Guid>>();
        c.AddCommandBehavior<PostPaymentCommand, Guid, AuthorizationPipelineBehavior<PostPaymentCommand, Guid>>();
        c.AddCommandBehavior<RecordRemittanceCommand, Guid, AuthorizationPipelineBehavior<RecordRemittanceCommand, Guid>>();

        // Integration
        c.AddCommandBehavior<IngestLabResultCommand, Guid, AuthorizationPipelineBehavior<IngestLabResultCommand, Guid>>();
    }
}
