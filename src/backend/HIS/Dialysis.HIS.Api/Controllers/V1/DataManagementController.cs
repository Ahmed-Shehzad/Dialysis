using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.DataServices.Features.GetDataImportJobById;
using Dialysis.HIS.DataServices.Features.ListIntegrationOutboxRecent;
using Dialysis.HIS.DataServices.Features.ManagerDashboard;
using Dialysis.HIS.DataServices.Features.SearchPatients;
using Dialysis.HIS.DataServices.Features.SubmitDataImportJob;
using Dialysis.HIS.DataServices.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Data management</em> — import jobs, outbox metadata, patient search (full-text corpus), manager dashboard.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/data-management")]
public sealed class DataManagementController : HisHateoasControllerBase
{
    private readonly ICqrsGateway _gateway;
    /// <summary>RA: <em>Data management</em> — import jobs, outbox metadata, patient search (full-text corpus), manager dashboard.</summary>
    public DataManagementController(ICqrsGateway gateway) => _gateway = gateway;
    [HttpPost("import-jobs")]
    [ProducesResponseType(typeof(ResourceEnvelope<SubmitDataImportJobResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitImportJobAsync([FromBody] SubmitDataImportJobCommand command, CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<SubmitDataImportJobCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource($"{Request.Path}/{id}", new SubmitDataImportJobResponse(id), LinkCapabilitiesIndex());
    }

    [HttpGet("import-jobs/{id:guid}")]
    [ProducesResponseType(typeof(ResourceEnvelope<DataImportJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImportJobAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _gateway
            .SendQueryAsync<GetDataImportJobByIdQuery, DataImportJobStatusDto?>(new GetDataImportJobByIdQuery(id), cancellationToken)
            .ConfigureAwait(false);
        return dto is null ? NotFound() : OkResource(dto, LinkCapabilitiesIndex());
    }

    /// <summary>Metadata-only view of recent Transponder transactional outbox rows (no payload body).</summary>
    [HttpGet("integration/outbox-metadata")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<IntegrationOutboxMetadataRow>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListIntegrationOutboxMetadataAsync([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var rows = await _gateway
            .SendQueryAsync<ListIntegrationOutboxRecentQuery, IReadOnlyList<IntegrationOutboxMetadataRow>>(
                new ListIntegrationOutboxRecentQuery(take),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(rows, LinkCapabilitiesIndex());
    }

    /// <summary>RA Fig. 6 — Data management → Search. Reads the <c>patients</c> corpus of the full-text index.</summary>
    [HttpGet("patients/search")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<PatientSearchRow>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPatientsAsync(
        [FromQuery] string? q,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await _gateway
            .SendQueryAsync<SearchPatientsQuery, IReadOnlyList<PatientSearchRow>>(
                new SearchPatientsQuery(q, skip, take),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(rows, LinkCapabilitiesIndex());
    }

    /// <summary>RA Fig. 6 — Generic MIS → Reporting. Operations workload snapshot (no PHI).</summary>
    [HttpGet("manager-dashboard")]
    [ProducesResponseType(typeof(ResourceEnvelope<ManagerDashboardSnapshotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ManagerDashboardAsync(
        [FromQuery] string? reportFocus,
        CancellationToken cancellationToken)
    {
        var snapshot = await _gateway
            .SendQueryAsync<ManagerDashboardQuery, ManagerDashboardSnapshotDto>(
                new ManagerDashboardQuery(reportFocus),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(snapshot, LinkCapabilitiesIndex());
    }

    public sealed record SubmitDataImportJobResponse
    {
        public SubmitDataImportJobResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = this.Id;
    }
}
