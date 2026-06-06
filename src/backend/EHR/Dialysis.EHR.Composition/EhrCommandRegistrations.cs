using Dialysis.CQRS;
using Dialysis.EHR.Billing.Features.CaptureCharge;
using Dialysis.EHR.Billing.Features.PostPayment;
using Dialysis.EHR.Billing.Features.RecordRemittance;
using Dialysis.EHR.Billing.Features.SubmitClaim;
using Dialysis.EHR.ClinicalNotes.Features.AttachDiagnosis;
using Dialysis.EHR.ClinicalNotes.Features.CloseEncounter;
using Dialysis.EHR.ClinicalNotes.Features.DraftClinicalNote;
using Dialysis.EHR.ClinicalNotes.Features.GetPatientReminders;
using Dialysis.EHR.ClinicalNotes.Features.ListImagingOrdersForPatient;
using Dialysis.EHR.ClinicalNotes.Features.ListLabResultsForPatient;
using Dialysis.EHR.ClinicalNotes.Features.ListReferralsForPatient;
using Dialysis.EHR.PatientChart.Features.CarePlanning;
using Dialysis.EHR.ClinicalNotes.Features.ListNotesForPatient;
using Dialysis.EHR.ClinicalNotes.Features.OrderImagingStudy;
using Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;
using Dialysis.EHR.ClinicalNotes.Features.ReviewImagingAiFinding;
using Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;
using Dialysis.EHR.ClinicalNotes.Features.OrderSets;
using Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;
using Dialysis.EHR.ClinicalNotes.Features.RequestReferral;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.EHR.ClinicalNotes.Features.SignClinicalNote;
using Dialysis.EHR.ClinicalNotes.Features.StartEncounter;
using Dialysis.EHR.Integration.Features.IngestLabResult;
using Dialysis.EHR.Integration.Features.MarkHospitalEventFollowedUp;
using Dialysis.EHR.Registration.Features.CareTeam;
using Dialysis.EHR.PatientChart.Features.GetPatientChart;
using Dialysis.EHR.PatientChart.Features.RecordAllergy;
using Dialysis.EHR.PatientChart.Features.RecordImmunization;
using Dialysis.EHR.PatientChart.Features.RecordMedicationStatement;
using Dialysis.EHR.PatientChart.Features.RecordProblem;
using Dialysis.EHR.PatientChart.Features.RecordVitalSign;
using Dialysis.EHR.PatientPortal.Features.AuthorAfterVisitSummary;
using Dialysis.EHR.PatientPortal.Features.CancelAppointmentRequest;
using Dialysis.EHR.PatientPortal.Features.ListAfterVisitSummaries;
using Dialysis.EHR.PatientPortal.Features.ListAppointmentRequests;
using Dialysis.EHR.PatientPortal.Features.ListMessageThreads;
using Dialysis.EHR.PatientPortal.Features.ListThreadMessages;
using Dialysis.EHR.PatientPortal.Features.MarkMessageRead;
using Dialysis.EHR.PatientPortal.Features.ReplySecureMessage;
using Dialysis.EHR.PatientPortal.Features.RequestAppointment;
using Dialysis.EHR.PatientPortal.Features.ResolveAppointmentRequest;
using Dialysis.EHR.PatientPortal.Features.SendSecureMessage;
using Dialysis.EHR.Registration.Features.GetPatientById;
using Dialysis.EHR.Registration.Features.MergePatients;
using Dialysis.EHR.Registration.Features.RegisterPatient;
using Dialysis.EHR.Registration.Features.RegisterProvider;
using Dialysis.EHR.Registration.Features.SearchPatients;
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
        c.AddCommandBehavior<CreateCarePlanCommand, Guid, AuthorizationPipelineBehavior<CreateCarePlanCommand, Guid>>();
        c.AddCommandBehavior<AddCarePlanGoalCommand, Guid, AuthorizationPipelineBehavior<AddCarePlanGoalCommand, Guid>>();
        c.AddCommandBehavior<UpdateCarePlanGoalStatusCommand, Guid, AuthorizationPipelineBehavior<UpdateCarePlanGoalStatusCommand, Guid>>();
        c.AddCommandBehavior<CloseCarePlanCommand, Guid, AuthorizationPipelineBehavior<CloseCarePlanCommand, Guid>>();

        // Scheduling
        c.AddCommandBehavior<BookAppointmentCommand, Guid, AuthorizationPipelineBehavior<BookAppointmentCommand, Guid>>();
        c.AddCommandBehavior<CancelAppointmentCommand, Unit, AuthorizationPipelineBehavior<CancelAppointmentCommand, Unit>>();
        c.AddCommandBehavior<RescheduleAppointmentCommand, Unit, AuthorizationPipelineBehavior<RescheduleAppointmentCommand, Unit>>();
        c.AddCommandBehavior<CheckInPatientCommand, Unit, AuthorizationPipelineBehavior<CheckInPatientCommand, Unit>>();

        // Portal
        c.AddCommandBehavior<RequestAppointmentCommand, Guid, AuthorizationPipelineBehavior<RequestAppointmentCommand, Guid>>();
        c.AddCommandBehavior<SendSecureMessageCommand, Guid, AuthorizationPipelineBehavior<SendSecureMessageCommand, Guid>>();
        c.AddCommandBehavior<ProviderReplyToMessageCommand, Guid, AuthorizationPipelineBehavior<ProviderReplyToMessageCommand, Guid>>();
        c.AddCommandBehavior<MarkMessageReadCommand, Unit, AuthorizationPipelineBehavior<MarkMessageReadCommand, Unit>>();
        c.AddCommandBehavior<ApproveAppointmentRequestCommand, Unit, AuthorizationPipelineBehavior<ApproveAppointmentRequestCommand, Unit>>();
        c.AddCommandBehavior<DeclineAppointmentRequestCommand, Unit, AuthorizationPipelineBehavior<DeclineAppointmentRequestCommand, Unit>>();
        c.AddCommandBehavior<CancelAppointmentRequestCommand, Unit, AuthorizationPipelineBehavior<CancelAppointmentRequestCommand, Unit>>();
        c.AddCommandBehavior<CreateAfterVisitSummaryCommand, Guid, AuthorizationPipelineBehavior<CreateAfterVisitSummaryCommand, Guid>>();
        c.AddCommandBehavior<AddAfterVisitSummaryLineCommand, Guid, AuthorizationPipelineBehavior<AddAfterVisitSummaryLineCommand, Guid>>();
        c.AddCommandBehavior<PublishAfterVisitSummaryCommand, Unit, AuthorizationPipelineBehavior<PublishAfterVisitSummaryCommand, Unit>>();

        // ClinicalNotes
        c.AddCommandBehavior<StartEncounterCommand, Guid, AuthorizationPipelineBehavior<StartEncounterCommand, Guid>>();
        c.AddCommandBehavior<AttachDiagnosisCommand, Unit, AuthorizationPipelineBehavior<AttachDiagnosisCommand, Unit>>();
        c.AddCommandBehavior<CloseEncounterCommand, Unit, AuthorizationPipelineBehavior<CloseEncounterCommand, Unit>>();
        c.AddCommandBehavior<DraftClinicalNoteCommand, Guid, AuthorizationPipelineBehavior<DraftClinicalNoteCommand, Guid>>();
        c.AddCommandBehavior<SignClinicalNoteCommand, Unit, AuthorizationPipelineBehavior<SignClinicalNoteCommand, Unit>>();
        c.AddCommandBehavior<OrderPrescriptionCommand, OrderPlacementResult, AuthorizationPipelineBehavior<OrderPrescriptionCommand, OrderPlacementResult>>();
        c.AddCommandBehavior<OrderLabTestCommand, OrderPlacementResult, AuthorizationPipelineBehavior<OrderLabTestCommand, OrderPlacementResult>>();
        c.AddCommandBehavior<OrderImagingStudyCommand, Guid, AuthorizationPipelineBehavior<OrderImagingStudyCommand, Guid>>();
        c.AddCommandBehavior<ReviewImagingAiFindingCommand, Unit, AuthorizationPipelineBehavior<ReviewImagingAiFindingCommand, Unit>>();
        c.AddCommandBehavior<RequestReferralCommand, Guid, AuthorizationPipelineBehavior<RequestReferralCommand, Guid>>();
        c.AddCommandBehavior<MarkHospitalEventFollowedUpCommand, Unit, AuthorizationPipelineBehavior<MarkHospitalEventFollowedUpCommand, Unit>>();
        c.AddCommandBehavior<AddCareTeamMemberCommand, Guid, AuthorizationPipelineBehavior<AddCareTeamMemberCommand, Guid>>();
        c.AddCommandBehavior<RemoveCareTeamMemberCommand, Unit, AuthorizationPipelineBehavior<RemoveCareTeamMemberCommand, Unit>>();
        c.AddCommandBehavior<SetPrimaryCareTeamMemberCommand, Unit, AuthorizationPipelineBehavior<SetPrimaryCareTeamMemberCommand, Unit>>();
        c.AddCommandBehavior<CreateOrderSetCommand, Guid, AuthorizationPipelineBehavior<CreateOrderSetCommand, Guid>>();
        c.AddCommandBehavior<DeactivateOrderSetCommand, Unit, AuthorizationPipelineBehavior<DeactivateOrderSetCommand, Unit>>();
        c.AddCommandBehavior<ApplyOrderSetCommand, ApplyOrderSetResult, AuthorizationPipelineBehavior<ApplyOrderSetCommand, ApplyOrderSetResult>>();

        // Billing
        c.AddCommandBehavior<CaptureChargeCommand, Guid, AuthorizationPipelineBehavior<CaptureChargeCommand, Guid>>();
        c.AddCommandBehavior<SubmitClaimCommand, Guid, AuthorizationPipelineBehavior<SubmitClaimCommand, Guid>>();
        c.AddCommandBehavior<PostPaymentCommand, Guid, AuthorizationPipelineBehavior<PostPaymentCommand, Guid>>();
        c.AddCommandBehavior<RecordRemittanceCommand, Guid, AuthorizationPipelineBehavior<RecordRemittanceCommand, Guid>>();

        // Integration
        c.AddCommandBehavior<IngestLabResultCommand, Guid, AuthorizationPipelineBehavior<IngestLabResultCommand, Guid>>();

        // Queries
        c.AddQueryBehavior<SearchPatientsQuery, PatientSearchResult, AuthorizationPipelineBehavior<SearchPatientsQuery, PatientSearchResult>>();
        c.AddQueryBehavior<GetPatientByIdQuery, PatientDetailDto?, AuthorizationPipelineBehavior<GetPatientByIdQuery, PatientDetailDto?>>();
        c.AddQueryBehavior<GetPatientChartQuery, PatientChartView, AuthorizationPipelineBehavior<GetPatientChartQuery, PatientChartView>>();
        c.AddQueryBehavior<ListNotesForPatientQuery, IReadOnlyList<ClinicalNoteListItem>, AuthorizationPipelineBehavior<ListNotesForPatientQuery, IReadOnlyList<ClinicalNoteListItem>>>();
        c.AddQueryBehavior<ListLabResultsForPatientQuery, IReadOnlyList<LabResultListItem>, AuthorizationPipelineBehavior<ListLabResultsForPatientQuery, IReadOnlyList<LabResultListItem>>>();
        c.AddQueryBehavior<ListImagingOrdersForPatientQuery, IReadOnlyList<ImagingOrderDto>, AuthorizationPipelineBehavior<ListImagingOrdersForPatientQuery, IReadOnlyList<ImagingOrderDto>>>();
        c.AddQueryBehavior<ListReferralsForPatientQuery, IReadOnlyList<ReferralDto>, AuthorizationPipelineBehavior<ListReferralsForPatientQuery, IReadOnlyList<ReferralDto>>>();
        c.AddQueryBehavior<GetActiveCarePlanQuery, CarePlanView?, AuthorizationPipelineBehavior<GetActiveCarePlanQuery, CarePlanView?>>();
        c.AddQueryBehavior<GetQualityGapsQuery, IReadOnlyList<QualityGap>, AuthorizationPipelineBehavior<GetQualityGapsQuery, IReadOnlyList<QualityGap>>>();
        c.AddQueryBehavior<EvaluateCohortQualityQuery, CohortQualityResult, AuthorizationPipelineBehavior<EvaluateCohortQualityQuery, CohortQualityResult>>();
        c.AddQueryBehavior<GetCareTeamQuery, CareTeamView?, AuthorizationPipelineBehavior<GetCareTeamQuery, CareTeamView?>>();
        c.AddQueryBehavior<ListOrderSetsQuery, IReadOnlyList<OrderSetView>, AuthorizationPipelineBehavior<ListOrderSetsQuery, IReadOnlyList<OrderSetView>>>();
        c.AddQueryBehavior<ListMessageThreadsForPatientQuery, IReadOnlyList<MessageThreadView>, AuthorizationPipelineBehavior<ListMessageThreadsForPatientQuery, IReadOnlyList<MessageThreadView>>>();
        c.AddQueryBehavior<ListThreadMessagesQuery, IReadOnlyList<SecureMessageView>, AuthorizationPipelineBehavior<ListThreadMessagesQuery, IReadOnlyList<SecureMessageView>>>();
        c.AddQueryBehavior<ListMyAppointmentRequestsQuery, IReadOnlyList<AppointmentRequestView>, AuthorizationPipelineBehavior<ListMyAppointmentRequestsQuery, IReadOnlyList<AppointmentRequestView>>>();
        c.AddQueryBehavior<ListPendingAppointmentRequestsQuery, IReadOnlyList<AppointmentRequestView>, AuthorizationPipelineBehavior<ListPendingAppointmentRequestsQuery, IReadOnlyList<AppointmentRequestView>>>();
        c.AddQueryBehavior<ListMyAfterVisitSummariesQuery, IReadOnlyList<AfterVisitSummaryView>, AuthorizationPipelineBehavior<ListMyAfterVisitSummariesQuery, IReadOnlyList<AfterVisitSummaryView>>>();
        c.AddQueryBehavior<GetAfterVisitSummaryByIdQuery, AfterVisitSummaryView?, AuthorizationPipelineBehavior<GetAfterVisitSummaryByIdQuery, AfterVisitSummaryView?>>();
        c.AddQueryBehavior<GetPatientRemindersQuery, IReadOnlyList<PatientReminderDto>, AuthorizationPipelineBehavior<GetPatientRemindersQuery, IReadOnlyList<PatientReminderDto>>>();
    }
}
