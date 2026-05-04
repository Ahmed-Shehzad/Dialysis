using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.RaCapabilities.Features;
using Dialysis.HIS.RaCapabilities.Features.ClearPatientAlert;
using Dialysis.HIS.RaCapabilities.Features.EnqueueWaitlistEntry;
using Dialysis.HIS.RaCapabilities.Features.PostOrganizationalCommunication;
using Dialysis.HIS.RaCapabilities.Features.RecordClinicalDecisionSupportEvaluation;
using Dialysis.HIS.RaCapabilities.Features.RecordSecurityMechanismAssessment;
using Dialysis.HIS.RaCapabilities.Features.ListResearchEducationActivities;
using Dialysis.HIS.RaCapabilities.Features.ListSpecialistEncounters;
using Dialysis.HIS.RaCapabilities.Features.RegisterEhrDocumentExchange;
using Dialysis.HIS.RaCapabilities.Features.RegisterResearchEducationActivity;
using Dialysis.HIS.RaCapabilities.Features.RegisterSpecialistEncounter;
using Dialysis.HIS.RaCapabilities.Features.RequestAnalyticsExportJob;
using Dialysis.HIS.RaCapabilities.Features.UpdateQualityWorkflowTaskStatus;
using Dialysis.HIS.RaCapabilities.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>
/// Fully implemented RA extension reads (Tummers et al., 2021) — Generic MIS, scheduling, patient monitoring,
/// medication, data management, and security mechanisms — backed by <c>his_ra</c> tables and CQRS queries.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reference-architecture/capabilities")]
public sealed class RaCapabilitiesController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    public sealed record CapabilityIndexDto(IReadOnlyList<LinkDto> Endpoints);

    [HttpGet("")]
    [ProducesResponseType(typeof(ResourceEnvelope<CapabilityIndexDto>), StatusCodes.Status200OK)]
    public IActionResult GetIndex()
    {
        var v = ApiVersionSegment;
        var basePath = $"/api/v{v}/reference-architecture/capabilities";
        LinkDto L(string rel, string sub) => LinkTo(rel, basePath + sub);
        var endpoints = new[]
        {
            L("ra:org-communications", "/generic-mis/organizational-communications"),
            L("ra:quality-workflows", "/generic-mis/quality-workflows"),
            L("ra:financial-erp", "/generic-mis/financial-erp-depth"),
            L("ra:waitlists", "/planning-and-scheduling/waitlists"),
            L("ra:ehr-documents", "/patient-monitoring/ehr-document-exchange"),
            L("ra:alerts", "/patient-monitoring/advanced-alerting"),
            L("ra:dispensing", "/medication-management/dispensing-and-barcode"),
            L("ra:cds", "/medication-management/clinical-decision-support"),
            L("ra:analytics-exports", "/data-management/analytics-exports"),
            L("ra:full-text", "/data-management/full-text-and-indexing"),
            L("ra:security-hardening", "/security/mechanisms-hardening"),
            L("ra:specialist-encounters", "/patient-monitoring/specialist-encounters"),
            L("ra:research-education", "/generic-mis/research-education"),
        };
        return OkResource(new CapabilityIndexDto(endpoints));
    }

    [HttpGet("generic-mis/organizational-communications")]
    public async Task<IActionResult> OrganizationalCommunications(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListOrganizationalCommunicationsQuery, IReadOnlyList<RaOrgCommunicationRow>>(new ListOrganizationalCommunicationsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities", "GET"));
    }

    [HttpPost("generic-mis/organizational-communications")]
    [ProducesResponseType(typeof(ResourceEnvelope<PostOrgCommunicationResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> PostOrganizationalCommunication(
        [FromBody] PostOrganizationalCommunicationCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<PostOrganizationalCommunicationCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new PostOrgCommunicationResponse(id),
            LinkTo("ra:org-communications", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/generic-mis/organizational-communications", "GET"));
    }

    public sealed record PostOrgCommunicationResponse(Guid Id);

    [HttpGet("generic-mis/quality-workflows")]
    public async Task<IActionResult> QualityWorkflows(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListQualityWorkflowTasksQuery, IReadOnlyList<RaQualityWorkflowTaskRow>>(new ListQualityWorkflowTasksQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities", "GET"));
    }

    [HttpPost("generic-mis/quality-workflows/{taskId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateQualityWorkflowTaskStatus(
        Guid taskId,
        [FromBody] QualityTaskStatusBody body,
        CancellationToken cancellationToken)
    {
        await gateway
            .SendCommandAsync<UpdateQualityWorkflowTaskStatusCommand, Unit>(
                new UpdateQualityWorkflowTaskStatusCommand(taskId, body.NewStatusCode),
                cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    public sealed record QualityTaskStatusBody(string NewStatusCode);

    [HttpGet("generic-mis/financial-erp-depth")]
    public async Task<IActionResult> FinancialErpDepth(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListFinancialErpLinksQuery, IReadOnlyList<RaFinancialErpLinkRow>>(new ListFinancialErpLinksQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities", "GET"));
    }

    [HttpGet("generic-mis/research-education")]
    public async Task<IActionResult> ResearchEducationActivities(CancellationToken cancellationToken)
    {
        var data = await gateway
            .SendQueryAsync<ListResearchEducationActivitiesQuery, IReadOnlyList<RaResearchEducationActivityRow>>(
                new ListResearchEducationActivitiesQuery(),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities", "GET"));
    }

    [HttpPost("generic-mis/research-education/activities")]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterResearchEducationActivityResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterResearchEducationActivity(
        [FromBody] RegisterResearchEducationActivityCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<RegisterResearchEducationActivityCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RegisterResearchEducationActivityResponse(id),
            LinkTo(
                "ra:research-education",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/generic-mis/research-education",
                "GET"));
    }

    public sealed record RegisterResearchEducationActivityResponse(Guid Id);

    [HttpGet("planning-and-scheduling/waitlists")]
    public async Task<IActionResult> Waitlists(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListWaitlistEntriesQuery, IReadOnlyList<RaWaitlistEntryRow>>(new ListWaitlistEntriesQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:scheduling", $"/api/v{ApiVersionSegment}/scheduling/resources", "GET"));
    }

    [HttpPost("planning-and-scheduling/waitlists")]
    [ProducesResponseType(typeof(ResourceEnvelope<EnqueueWaitlistResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> EnqueueWaitlist([FromBody] EnqueueWaitlistEntryCommand command, CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<EnqueueWaitlistEntryCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new EnqueueWaitlistResponse(id),
            LinkTo("ra:waitlists", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/planning-and-scheduling/waitlists", "GET"));
    }

    public sealed record EnqueueWaitlistResponse(Guid Id);

    [HttpGet("patient-monitoring/ehr-document-exchange")]
    public async Task<IActionResult> EhrDocumentExchange(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListEhrDocumentExchangesQuery, IReadOnlyList<RaEhrDocumentExchangeRow>>(new ListEhrDocumentExchangesQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities", "GET"));
    }

    [HttpPost("patient-monitoring/ehr-document-exchange/records")]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterEhrDocumentResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterEhrDocumentExchangeRecord(
        [FromBody] RegisterEhrDocumentExchangeCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<RegisterEhrDocumentExchangeCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RegisterEhrDocumentResponse(id),
            LinkTo(
                "ra:ehr-documents",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/patient-monitoring/ehr-document-exchange",
                "GET"));
    }

    public sealed record RegisterEhrDocumentResponse(Guid Id);

    [HttpGet("patient-monitoring/specialist-encounters")]
    public async Task<IActionResult> SpecialistEncounters(CancellationToken cancellationToken)
    {
        var data = await gateway
            .SendQueryAsync<ListSpecialistEncountersQuery, IReadOnlyList<RaSpecialistEncounterRow>>(
                new ListSpecialistEncountersQuery(),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities", "GET"));
    }

    [HttpPost("patient-monitoring/specialist-encounters/records")]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterSpecialistEncounterResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterSpecialistEncounterRecord(
        [FromBody] RegisterSpecialistEncounterCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<RegisterSpecialistEncounterCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RegisterSpecialistEncounterResponse(id),
            LinkTo(
                "ra:specialist-encounters",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/patient-monitoring/specialist-encounters",
                "GET"));
    }

    public sealed record RegisterSpecialistEncounterResponse(Guid Id);

    [HttpGet("patient-monitoring/advanced-alerting")]
    public async Task<IActionResult> AdvancedAlerting(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListPatientAlertsQuery, IReadOnlyList<RaPatientAlertRow>>(new ListPatientAlertsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities", "GET"));
    }

    [HttpPost("patient-monitoring/advanced-alerting/{alertId:guid}/clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearAlert(Guid alertId, CancellationToken cancellationToken)
    {
        _ = await gateway
            .SendCommandAsync<ClearPatientAlertCommand, Unit>(new ClearPatientAlertCommand(alertId), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("medication-management/dispensing-and-barcode")]
    public async Task<IActionResult> Dispensing(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListMedicationDispensingRecordsQuery, IReadOnlyList<RaMedicationDispensingRow>>(new ListMedicationDispensingRecordsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:medication-orders", $"/api/v{ApiVersionSegment}/medication/orders", "POST"));
    }

    [HttpGet("medication-management/clinical-decision-support")]
    public async Task<IActionResult> ClinicalDecisionSupport(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListClinicalDecisionSupportEvaluationsQuery, IReadOnlyList<RaClinicalDecisionSupportRow>>(new ListClinicalDecisionSupportEvaluationsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:medication-orders", $"/api/v{ApiVersionSegment}/medication/orders", "POST"));
    }

    [HttpPost("medication-management/clinical-decision-support/evaluations")]
    [ProducesResponseType(typeof(ResourceEnvelope<RecordCdsResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordClinicalDecisionSupportEvaluation(
        [FromBody] RecordClinicalDecisionSupportEvaluationCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<RecordClinicalDecisionSupportEvaluationCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RecordCdsResponse(id),
            LinkTo(
                "ra:cds",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/medication-management/clinical-decision-support",
                "GET"));
    }

    public sealed record RecordCdsResponse(Guid Id);

    [HttpGet("data-management/analytics-exports")]
    public async Task<IActionResult> AnalyticsExports(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListAnalyticsExportJobsQuery, IReadOnlyList<RaAnalyticsExportJobRow>>(new ListAnalyticsExportJobsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:data-import", $"/api/v{ApiVersionSegment}/data-management/import-jobs", "POST"));
    }

    [HttpPost("data-management/analytics-export-jobs")]
    [ProducesResponseType(typeof(ResourceEnvelope<RequestAnalyticsExportResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestAnalyticsExportJob(
        [FromBody] RequestAnalyticsExportJobCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<RequestAnalyticsExportJobCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RequestAnalyticsExportResponse(id),
            LinkTo("ra:analytics-exports", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/data-management/analytics-exports", "GET"));
    }

    public sealed record RequestAnalyticsExportResponse(Guid Id);

    [HttpGet("data-management/full-text-and-indexing")]
    public async Task<IActionResult> FullTextIndexing([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var data = await gateway
            .SendQueryAsync<ListFullTextSearchEntriesQuery, IReadOnlyList<RaFullTextSearchEntryRow>>(
                new ListFullTextSearchEntriesQuery(q),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:patient-search", $"/api/v{ApiVersionSegment}/data-management/patients/search", "GET"));
    }

    [HttpGet("security/mechanisms-hardening")]
    public async Task<IActionResult> SecurityMechanisms(CancellationToken cancellationToken)
    {
        var data = await gateway.SendQueryAsync<ListSecurityMechanismHardeningsQuery, IReadOnlyList<RaSecurityMechanismRow>>(new ListSecurityMechanismHardeningsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:security-users", $"/api/v{ApiVersionSegment}/security/users", "POST"));
    }

    [HttpPost("security/mechanisms-hardening/assessments")]
    [ProducesResponseType(typeof(ResourceEnvelope<RecordSecurityAssessmentResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordSecurityMechanismAssessment(
        [FromBody] RecordSecurityMechanismAssessmentCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<RecordSecurityMechanismAssessmentCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RecordSecurityAssessmentResponse(id),
            LinkTo(
                "ra:security-hardening",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/security/mechanisms-hardening",
                "GET"));
    }

    public sealed record RecordSecurityAssessmentResponse(Guid Id);
}
