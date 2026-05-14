using Dialysis.CQRS;
using Dialysis.HIS.DataServices;
using Dialysis.HIS.DataServices.Features.GetDataImportJobById;
using Dialysis.HIS.DataServices.Features.ListIntegrationOutboxRecent;
using Dialysis.HIS.DataServices.Features.ManagerDashboard;
using Dialysis.HIS.DataServices.Features.SearchPatients;
using Dialysis.HIS.DataServices.Features.SubmitDataImportJob;
using Dialysis.HIS.DataServices.Ports;
using Dialysis.HIS.Integration;
using Dialysis.HIS.Integration.Features.IngestDeviceReading;
using Dialysis.HIS.Medication;
using Dialysis.HIS.Medication.Features.PlaceMedicationOrder;
using Dialysis.HIS.Operations;
using Dialysis.HIS.Operations.Features.AssignStaffRole;
using Dialysis.HIS.Operations.Features.GetBillingExportJobById;
using Dialysis.HIS.Operations.Features.RecordInventoryMovement;
using Dialysis.HIS.Operations.Features.SubmitBillingExportJob;
using Dialysis.HIS.PatientAccess;
using Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;
using Dialysis.HIS.PatientFlow;
using Dialysis.HIS.PatientFlow.Features.AdmitPatient;
using Dialysis.HIS.RaCapabilities;
using Dialysis.HIS.Scheduling;
using Dialysis.HIS.Scheduling.Features.BookAppointment;
using Dialysis.HIS.Security;
using Dialysis.HIS.Security.Features.RegisterLocalUser;
using Dialysis.HIS.RaCapabilities.Features;
using Dialysis.HIS.RaCapabilities.Features.ClearPatientAlert;
using Dialysis.HIS.RaCapabilities.Features.EnqueueWaitlistEntry;
using Dialysis.HIS.RaCapabilities.Features.PostOrganizationalCommunication;
using Dialysis.HIS.RaCapabilities.Features.RecordClinicalDecisionSupportEvaluation;
using Dialysis.HIS.RaCapabilities.Features.RecordMedicationDispensing;
using Dialysis.HIS.RaCapabilities.Features.RecordSecurityMechanismAssessment;
using Dialysis.HIS.RaCapabilities.Features.ListResearchEducationActivities;
using Dialysis.HIS.RaCapabilities.Features.ListSpecialistEncounters;
using Dialysis.HIS.RaCapabilities.Features.RegisterEhrDocumentExchange;
using Dialysis.HIS.RaCapabilities.Features.RegisterFinancialErpLink;
using Dialysis.HIS.RaCapabilities.Features.RegisterResearchEducationActivity;
using Dialysis.HIS.RaCapabilities.Features.RegisterSpecialistEncounter;
using Dialysis.HIS.RaCapabilities.Features.RequestAnalyticsExportJob;
using Dialysis.HIS.RaCapabilities.Features.UpdateQualityWorkflowTaskStatus;
using Dialysis.HIS.RaCapabilities.Ports;
using Dialysis.Module.Hosting.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Composition;

/// <summary>
/// Wires the shared <see cref="Dialysis.CQRS"/> library into HIS: <see cref="CqrsServiceCollectionExtensions.AddCqrs"/>,
/// assembly scanning for handlers/validators, and authorization pipeline behaviors for permissioned messages.
/// Clinical/scheduling/medication/billing commands have moved to the EHR module.
/// </summary>
public static class HisCqrsServiceCollectionExtensions
{
    public static IServiceCollection AddHisCqrs(this IServiceCollection services) =>
        services.AddCqrs(cqrs =>
        {
            cqrs.AddFromAssembliesOf(
                typeof(HisOperationsMarker),
                typeof(HisDataServicesMarker),
                typeof(HisIntegrationMarker),
                typeof(RaCapabilitiesMarker),
                typeof(HisSecurityMarker),
                typeof(HisSchedulingMarker),
                typeof(HisPatientFlowMarker),
                typeof(HisPatientAccessMarker),
                typeof(HisMedicationMarker));

            cqrs.AddCommandBehavior<AssignStaffPrimaryRoleCommand, Unit, AuthorizationPipelineBehavior<AssignStaffPrimaryRoleCommand, Unit>>();
            cqrs.AddCommandBehavior<RecordInventoryMovementCommand, Unit, AuthorizationPipelineBehavior<RecordInventoryMovementCommand, Unit>>();
            cqrs.AddCommandBehavior<SubmitBillingExportJobCommand, Guid, AuthorizationPipelineBehavior<SubmitBillingExportJobCommand, Guid>>();
            cqrs.AddCommandBehavior<SubmitDataImportJobCommand, Guid, AuthorizationPipelineBehavior<SubmitDataImportJobCommand, Guid>>();
            cqrs.AddQueryBehavior<GetDataImportJobByIdQuery, DataImportJobStatusDto?, AuthorizationPipelineBehavior<GetDataImportJobByIdQuery, DataImportJobStatusDto?>>();
            cqrs.AddQueryBehavior<ListIntegrationOutboxRecentQuery, IReadOnlyList<IntegrationOutboxMetadataRow>, AuthorizationPipelineBehavior<ListIntegrationOutboxRecentQuery, IReadOnlyList<IntegrationOutboxMetadataRow>>>();
            cqrs.AddQueryBehavior<SearchPatientsQuery, IReadOnlyList<PatientSearchRow>, AuthorizationPipelineBehavior<SearchPatientsQuery, IReadOnlyList<PatientSearchRow>>>();
            cqrs.AddQueryBehavior<ManagerDashboardQuery, ManagerDashboardSnapshotDto, AuthorizationPipelineBehavior<ManagerDashboardQuery, ManagerDashboardSnapshotDto>>();
            cqrs.AddCommandBehavior<IngestDeviceReadingCommand, Guid, AuthorizationPipelineBehavior<IngestDeviceReadingCommand, Guid>>();

            cqrs.AddQueryBehavior<ListOrganizationalCommunicationsQuery, IReadOnlyList<RaOrgCommunicationRow>, AuthorizationPipelineBehavior<ListOrganizationalCommunicationsQuery, IReadOnlyList<RaOrgCommunicationRow>>>();
            cqrs.AddQueryBehavior<ListQualityWorkflowTasksQuery, IReadOnlyList<RaQualityWorkflowTaskRow>, AuthorizationPipelineBehavior<ListQualityWorkflowTasksQuery, IReadOnlyList<RaQualityWorkflowTaskRow>>>();
            cqrs.AddQueryBehavior<ListFinancialErpLinksQuery, IReadOnlyList<RaFinancialErpLinkRow>, AuthorizationPipelineBehavior<ListFinancialErpLinksQuery, IReadOnlyList<RaFinancialErpLinkRow>>>();
            cqrs.AddQueryBehavior<ListWaitlistEntriesQuery, IReadOnlyList<RaWaitlistEntryRow>, AuthorizationPipelineBehavior<ListWaitlistEntriesQuery, IReadOnlyList<RaWaitlistEntryRow>>>();
            cqrs.AddQueryBehavior<ListEhrDocumentExchangesQuery, IReadOnlyList<RaEhrDocumentExchangeRow>, AuthorizationPipelineBehavior<ListEhrDocumentExchangesQuery, IReadOnlyList<RaEhrDocumentExchangeRow>>>();
            cqrs.AddQueryBehavior<ListPatientAlertsQuery, IReadOnlyList<RaPatientAlertRow>, AuthorizationPipelineBehavior<ListPatientAlertsQuery, IReadOnlyList<RaPatientAlertRow>>>();
            cqrs.AddQueryBehavior<ListMedicationDispensingRecordsQuery, IReadOnlyList<RaMedicationDispensingRow>, AuthorizationPipelineBehavior<ListMedicationDispensingRecordsQuery, IReadOnlyList<RaMedicationDispensingRow>>>();
            cqrs.AddQueryBehavior<ListClinicalDecisionSupportEvaluationsQuery, IReadOnlyList<RaClinicalDecisionSupportRow>, AuthorizationPipelineBehavior<ListClinicalDecisionSupportEvaluationsQuery, IReadOnlyList<RaClinicalDecisionSupportRow>>>();
            cqrs.AddQueryBehavior<ListAnalyticsExportJobsQuery, IReadOnlyList<RaAnalyticsExportJobRow>, AuthorizationPipelineBehavior<ListAnalyticsExportJobsQuery, IReadOnlyList<RaAnalyticsExportJobRow>>>();
            cqrs.AddQueryBehavior<ListFullTextSearchEntriesQuery, IReadOnlyList<RaFullTextSearchEntryRow>, AuthorizationPipelineBehavior<ListFullTextSearchEntriesQuery, IReadOnlyList<RaFullTextSearchEntryRow>>>();
            cqrs.AddQueryBehavior<ListSecurityMechanismHardeningsQuery, IReadOnlyList<RaSecurityMechanismRow>, AuthorizationPipelineBehavior<ListSecurityMechanismHardeningsQuery, IReadOnlyList<RaSecurityMechanismRow>>>();
            cqrs.AddQueryBehavior<ListSpecialistEncountersQuery, IReadOnlyList<RaSpecialistEncounterRow>, AuthorizationPipelineBehavior<ListSpecialistEncountersQuery, IReadOnlyList<RaSpecialistEncounterRow>>>();
            cqrs.AddQueryBehavior<ListResearchEducationActivitiesQuery, IReadOnlyList<RaResearchEducationActivityRow>, AuthorizationPipelineBehavior<ListResearchEducationActivitiesQuery, IReadOnlyList<RaResearchEducationActivityRow>>>();
            cqrs.AddCommandBehavior<EnqueueWaitlistEntryCommand, Guid, AuthorizationPipelineBehavior<EnqueueWaitlistEntryCommand, Guid>>();
            cqrs.AddCommandBehavior<ClearPatientAlertCommand, Unit, AuthorizationPipelineBehavior<ClearPatientAlertCommand, Unit>>();
            cqrs.AddCommandBehavior<RecordClinicalDecisionSupportEvaluationCommand, Guid, AuthorizationPipelineBehavior<RecordClinicalDecisionSupportEvaluationCommand, Guid>>();
            cqrs.AddCommandBehavior<PostOrganizationalCommunicationCommand, Guid, AuthorizationPipelineBehavior<PostOrganizationalCommunicationCommand, Guid>>();
            cqrs.AddCommandBehavior<RequestAnalyticsExportJobCommand, Guid, AuthorizationPipelineBehavior<RequestAnalyticsExportJobCommand, Guid>>();
            cqrs.AddCommandBehavior<RegisterEhrDocumentExchangeCommand, Guid, AuthorizationPipelineBehavior<RegisterEhrDocumentExchangeCommand, Guid>>();
            cqrs.AddCommandBehavior<UpdateQualityWorkflowTaskStatusCommand, Unit, AuthorizationPipelineBehavior<UpdateQualityWorkflowTaskStatusCommand, Unit>>();
            cqrs.AddCommandBehavior<RecordSecurityMechanismAssessmentCommand, Guid, AuthorizationPipelineBehavior<RecordSecurityMechanismAssessmentCommand, Guid>>();
            cqrs.AddCommandBehavior<RegisterSpecialistEncounterCommand, Guid, AuthorizationPipelineBehavior<RegisterSpecialistEncounterCommand, Guid>>();
            cqrs.AddCommandBehavior<RegisterResearchEducationActivityCommand, Guid, AuthorizationPipelineBehavior<RegisterResearchEducationActivityCommand, Guid>>();
            cqrs.AddCommandBehavior<RegisterFinancialErpLinkCommand, Guid, AuthorizationPipelineBehavior<RegisterFinancialErpLinkCommand, Guid>>();
            cqrs.AddCommandBehavior<RecordMedicationDispensingCommand, Guid, AuthorizationPipelineBehavior<RecordMedicationDispensingCommand, Guid>>();

            cqrs.AddCommandBehavior<RegisterLocalUserCommand, Guid, AuthorizationPipelineBehavior<RegisterLocalUserCommand, Guid>>();
            cqrs.AddCommandBehavior<BookAppointmentCommand, Guid, AuthorizationPipelineBehavior<BookAppointmentCommand, Guid>>();
            cqrs.AddCommandBehavior<AdmitPatientCommand, Guid, AuthorizationPipelineBehavior<AdmitPatientCommand, Guid>>();
            cqrs.AddCommandBehavior<PlaceMedicationOrderCommand, Guid, AuthorizationPipelineBehavior<PlaceMedicationOrderCommand, Guid>>();
            cqrs.AddQueryBehavior<GetPatientPortalSummaryQuery, PatientPortalSummaryDto, AuthorizationPipelineBehavior<GetPatientPortalSummaryQuery, PatientPortalSummaryDto>>();
        });
}
