using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.RaCapabilities.Features;
using Dialysis.HIS.RaCapabilities.Features.ClearPatientAlert;
using Dialysis.HIS.RaCapabilities.Features.EnqueueWaitlistEntry;
using Dialysis.HIS.RaCapabilities.Features.ListResearchEducationActivities;
using Dialysis.HIS.RaCapabilities.Features.ListSpecialistEncounters;
using Dialysis.HIS.RaCapabilities.Features.PostOrganizationalCommunication;
using Dialysis.HIS.RaCapabilities.Features.RecordClinicalDecisionSupportEvaluation;
using Dialysis.HIS.RaCapabilities.Features.RecordMedicationDispensing;
using Dialysis.HIS.RaCapabilities.Features.RecordSecurityMechanismAssessment;
using Dialysis.HIS.RaCapabilities.Features.RegisterEhrDocumentExchange;
using Dialysis.HIS.RaCapabilities.Features.RegisterFinancialErpLink;
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
public sealed class RaCapabilitiesController : HisHateoasControllerBase
{
    private readonly ICqrsGateway _gateway;
    /// <summary>
    /// Fully implemented RA extension reads (Tummers et al., 2021) — Generic MIS, scheduling, patient monitoring,
    /// medication, data management, and security mechanisms — backed by <c>his_ra</c> tables and CQRS queries.
    /// </summary>
    public RaCapabilitiesController(ICqrsGateway gateway) => _gateway = gateway;

    public sealed record CapabilityIndexDto
    {
        public CapabilityIndexDto(IReadOnlyList<LinkDto> Endpoints) => this.Endpoints = Endpoints;
        public IReadOnlyList<LinkDto> Endpoints { get; init; }
        public void Deconstruct(out IReadOnlyList<LinkDto> endpoints) => endpoints = Endpoints;
    }

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
    public async Task<IActionResult> OrganizationalCommunicationsAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListOrganizationalCommunicationsQuery, IReadOnlyList<RaOrgCommunicationRow>>(new ListOrganizationalCommunicationsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities"));
    }

    [HttpPost("generic-mis/organizational-communications")]
    [ProducesResponseType(typeof(ResourceEnvelope<PostOrgCommunicationResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> PostOrganizationalCommunicationAsync(
        [FromBody] PostOrganizationalCommunicationCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<PostOrganizationalCommunicationCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new PostOrgCommunicationResponse(id),
            LinkTo("ra:org-communications", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/generic-mis/organizational-communications"));
    }

    public sealed record PostOrgCommunicationResponse
    {
        public PostOrgCommunicationResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    [HttpGet("generic-mis/quality-workflows")]
    public async Task<IActionResult> QualityWorkflowsAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListQualityWorkflowTasksQuery, IReadOnlyList<RaQualityWorkflowTaskRow>>(new ListQualityWorkflowTasksQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities"));
    }

    [HttpPost("generic-mis/quality-workflows/{taskId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateQualityWorkflowTaskStatusAsync(
        Guid taskId,
        [FromBody] QualityTaskStatusBody body,
        CancellationToken cancellationToken)
    {
        await _gateway
            .SendCommandAsync<UpdateQualityWorkflowTaskStatusCommand, Unit>(
                new UpdateQualityWorkflowTaskStatusCommand(taskId, body.NewStatusCode),
                cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    public sealed record QualityTaskStatusBody
    {
        public QualityTaskStatusBody(string NewStatusCode) => this.NewStatusCode = NewStatusCode;
        public string NewStatusCode { get; init; }
        public void Deconstruct(out string newStatusCode) => newStatusCode = NewStatusCode;
    }

    [HttpGet("generic-mis/financial-erp-depth")]
    public async Task<IActionResult> FinancialErpDepthAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListFinancialErpLinksQuery, IReadOnlyList<RaFinancialErpLinkRow>>(new ListFinancialErpLinksQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities"));
    }

    [HttpPost("generic-mis/financial-erp-depth/records")]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterFinancialErpLinkResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterFinancialErpLinkAsync(
        [FromBody] RegisterFinancialErpLinkCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway
            .SendCommandAsync<RegisterFinancialErpLinkCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RegisterFinancialErpLinkResponse(id),
            LinkTo(
                "ra:financial-erp",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/generic-mis/financial-erp-depth"));
    }

    public sealed record RegisterFinancialErpLinkResponse
    {
        public RegisterFinancialErpLinkResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    [HttpGet("generic-mis/research-education")]
    public async Task<IActionResult> ResearchEducationActivitiesAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway
            .SendQueryAsync<ListResearchEducationActivitiesQuery, IReadOnlyList<RaResearchEducationActivityRow>>(
                new ListResearchEducationActivitiesQuery(),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities"));
    }

    [HttpPost("generic-mis/research-education/activities")]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterResearchEducationActivityResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterResearchEducationActivityAsync(
        [FromBody] RegisterResearchEducationActivityCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway
            .SendCommandAsync<RegisterResearchEducationActivityCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RegisterResearchEducationActivityResponse(id),
            LinkTo(
                "ra:research-education",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/generic-mis/research-education"));
    }

    public sealed record RegisterResearchEducationActivityResponse
    {
        public RegisterResearchEducationActivityResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    [HttpGet("planning-and-scheduling/waitlists")]
    public async Task<IActionResult> WaitlistsAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListWaitlistEntriesQuery, IReadOnlyList<RaWaitlistEntryRow>>(new ListWaitlistEntriesQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:scheduling", $"/api/v{ApiVersionSegment}/scheduling/resources"));
    }

    [HttpPost("planning-and-scheduling/waitlists")]
    [ProducesResponseType(typeof(ResourceEnvelope<EnqueueWaitlistResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> EnqueueWaitlistAsync([FromBody] EnqueueWaitlistEntryCommand command, CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<EnqueueWaitlistEntryCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new EnqueueWaitlistResponse(id),
            LinkTo("ra:waitlists", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/planning-and-scheduling/waitlists"));
    }

    public sealed record EnqueueWaitlistResponse
    {
        public EnqueueWaitlistResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    [HttpGet("patient-monitoring/ehr-document-exchange")]
    public async Task<IActionResult> EhrDocumentExchangeAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListEhrDocumentExchangesQuery, IReadOnlyList<RaEhrDocumentExchangeRow>>(new ListEhrDocumentExchangesQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities"));
    }

    [HttpPost("patient-monitoring/ehr-document-exchange/records")]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterEhrDocumentResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterEhrDocumentExchangeRecordAsync(
        [FromBody] RegisterEhrDocumentExchangeCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway
            .SendCommandAsync<RegisterEhrDocumentExchangeCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RegisterEhrDocumentResponse(id),
            LinkTo(
                "ra:ehr-documents",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/patient-monitoring/ehr-document-exchange"));
    }

    public sealed record RegisterEhrDocumentResponse
    {
        public RegisterEhrDocumentResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    [HttpGet("patient-monitoring/specialist-encounters")]
    public async Task<IActionResult> SpecialistEncountersAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway
            .SendQueryAsync<ListSpecialistEncountersQuery, IReadOnlyList<RaSpecialistEncounterRow>>(
                new ListSpecialistEncountersQuery(),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities"));
    }

    [HttpPost("patient-monitoring/specialist-encounters/records")]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterSpecialistEncounterResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterSpecialistEncounterRecordAsync(
        [FromBody] RegisterSpecialistEncounterCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway
            .SendCommandAsync<RegisterSpecialistEncounterCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RegisterSpecialistEncounterResponse(id),
            LinkTo(
                "ra:specialist-encounters",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/patient-monitoring/specialist-encounters"));
    }

    public sealed record RegisterSpecialistEncounterResponse
    {
        public RegisterSpecialistEncounterResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    [HttpGet("patient-monitoring/advanced-alerting")]
    public async Task<IActionResult> AdvancedAlertingAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListPatientAlertsQuery, IReadOnlyList<RaPatientAlertRow>>(new ListPatientAlertsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:capability-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities"));
    }

    [HttpPost("patient-monitoring/advanced-alerting/{alertId:guid}/clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearAlertAsync(Guid alertId, CancellationToken cancellationToken)
    {
        _ = await _gateway
            .SendCommandAsync<ClearPatientAlertCommand, Unit>(new ClearPatientAlertCommand(alertId), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("medication-management/dispensing-and-barcode")]
    public async Task<IActionResult> DispensingAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListMedicationDispensingRecordsQuery, IReadOnlyList<RaMedicationDispensingRow>>(new ListMedicationDispensingRecordsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:medication-orders", $"/api/v{ApiVersionSegment}/medication/orders", "POST"));
    }

    [HttpPost("medication-management/dispensing-and-barcode/records")]
    [ProducesResponseType(typeof(ResourceEnvelope<RecordMedicationDispensingResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordMedicationDispensingAsync(
        [FromBody] RecordMedicationDispensingCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway
            .SendCommandAsync<RecordMedicationDispensingCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RecordMedicationDispensingResponse(id),
            LinkTo(
                "ra:dispensing",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/medication-management/dispensing-and-barcode"));
    }

    public sealed record RecordMedicationDispensingResponse
    {
        public RecordMedicationDispensingResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    [HttpGet("medication-management/clinical-decision-support")]
    public async Task<IActionResult> ClinicalDecisionSupportAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListClinicalDecisionSupportEvaluationsQuery, IReadOnlyList<RaClinicalDecisionSupportRow>>(new ListClinicalDecisionSupportEvaluationsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:medication-orders", $"/api/v{ApiVersionSegment}/medication/orders", "POST"));
    }

    [HttpPost("medication-management/clinical-decision-support/evaluations")]
    [ProducesResponseType(typeof(ResourceEnvelope<RecordCdsResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordClinicalDecisionSupportEvaluationAsync(
        [FromBody] RecordClinicalDecisionSupportEvaluationCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway
            .SendCommandAsync<RecordClinicalDecisionSupportEvaluationCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RecordCdsResponse(id),
            LinkTo(
                "ra:cds",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/medication-management/clinical-decision-support"));
    }

    public sealed record RecordCdsResponse
    {
        public RecordCdsResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    [HttpGet("data-management/analytics-exports")]
    public async Task<IActionResult> AnalyticsExportsAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListAnalyticsExportJobsQuery, IReadOnlyList<RaAnalyticsExportJobRow>>(new ListAnalyticsExportJobsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:data-import", $"/api/v{ApiVersionSegment}/data-management/import-jobs", "POST"));
    }

    [HttpPost("data-management/analytics-export-jobs")]
    [ProducesResponseType(typeof(ResourceEnvelope<RequestAnalyticsExportResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestAnalyticsExportJobAsync(
        [FromBody] RequestAnalyticsExportJobCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<RequestAnalyticsExportJobCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RequestAnalyticsExportResponse(id),
            LinkTo("ra:analytics-exports", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/data-management/analytics-exports"));
    }

    public sealed record RequestAnalyticsExportResponse
    {
        public RequestAnalyticsExportResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    [HttpGet("data-management/full-text-and-indexing")]
    public async Task<IActionResult> FullTextIndexingAsync([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var data = await _gateway
            .SendQueryAsync<ListFullTextSearchEntriesQuery, IReadOnlyList<RaFullTextSearchEntryRow>>(
                new ListFullTextSearchEntriesQuery(q),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:patient-search", $"/api/v{ApiVersionSegment}/data-management/patients/search"));
    }

    [HttpGet("security/mechanisms-hardening")]
    public async Task<IActionResult> SecurityMechanismsAsync(CancellationToken cancellationToken)
    {
        var data = await _gateway.SendQueryAsync<ListSecurityMechanismHardeningsQuery, IReadOnlyList<RaSecurityMechanismRow>>(new ListSecurityMechanismHardeningsQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(data, LinkTo("ra:security-users", $"/api/v{ApiVersionSegment}/security/users", "POST"));
    }

    [HttpPost("security/mechanisms-hardening/assessments")]
    [ProducesResponseType(typeof(ResourceEnvelope<RecordSecurityAssessmentResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordSecurityMechanismAssessmentAsync(
        [FromBody] RecordSecurityMechanismAssessmentCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway
            .SendCommandAsync<RecordSecurityMechanismAssessmentCommand, Guid>(command, cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource(
            $"{Request.Path}/{id}",
            new RecordSecurityAssessmentResponse(id),
            LinkTo(
                "ra:security-hardening",
                $"/api/v{ApiVersionSegment}/reference-architecture/capabilities/security/mechanisms-hardening"));
    }

    public sealed record RecordSecurityAssessmentResponse
    {
        public RecordSecurityAssessmentResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }
}
