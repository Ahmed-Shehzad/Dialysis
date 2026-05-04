using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.DataServices.Features.GetDataImportJobById;
using Dialysis.HIS.DataServices.Features.ListIntegrationOutboxRecent;
using Dialysis.HIS.DataServices.Features.ManagerDashboard;
using Dialysis.HIS.DataServices.Features.SearchPatients;
using Dialysis.HIS.DataServices.Features.SubmitDataImportJob;
using Dialysis.HIS.DataServices.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Data management</em> — import, search, analytics (Tummers et al., 2021).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/data-management")]
public sealed class DataManagementController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpGet("patients/search")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<PatientSearchResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPatients([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var results = await gateway
            .SendQueryAsync<SearchPatientsQuery, IReadOnlyList<PatientSearchResultDto>>(
                new SearchPatientsQuery(string.IsNullOrWhiteSpace(q) ? null : q),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(results, LinkCapabilitiesIndex());
    }

    [HttpPost("import-jobs")]
    [ProducesResponseType(typeof(ResourceEnvelope<SubmitDataImportJobResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitImportJob([FromBody] SubmitDataImportJobCommand command, CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<SubmitDataImportJobCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource($"{Request.Path}/{id}", new SubmitDataImportJobResponse(id), LinkCapabilitiesIndex());
    }

    [HttpGet("import-jobs/{id:guid}")]
    [ProducesResponseType(typeof(ResourceEnvelope<DataImportJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImportJob(Guid id, CancellationToken cancellationToken)
    {
        var dto = await gateway
            .SendQueryAsync<GetDataImportJobByIdQuery, DataImportJobStatusDto?>(new GetDataImportJobByIdQuery(id), cancellationToken)
            .ConfigureAwait(false);
        return dto is null ? NotFound() : OkResource(dto, LinkCapabilitiesIndex());
    }

    [HttpGet("manager-dashboard")]
    [ProducesResponseType(typeof(ResourceEnvelope<ManagerDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetManagerDashboard([FromQuery] string? reportFocus, CancellationToken cancellationToken)
    {
        var dto = await gateway
            .SendQueryAsync<ManagerDashboardQuery, ManagerDashboardDto>(new ManagerDashboardQuery(reportFocus), cancellationToken)
            .ConfigureAwait(false);
        return OkResource(dto, LinkCapabilitiesIndex());
    }

    /// <summary>Metadata-only view of recent Transponder transactional outbox rows (no payload body).</summary>
    [HttpGet("integration/outbox-metadata")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<IntegrationOutboxMetadataRow>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListIntegrationOutboxMetadata([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var rows = await gateway
            .SendQueryAsync<ListIntegrationOutboxRecentQuery, IReadOnlyList<IntegrationOutboxMetadataRow>>(
                new ListIntegrationOutboxRecentQuery(take),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(rows, LinkCapabilitiesIndex());
    }

    public sealed record SubmitDataImportJobResponse(Guid Id);
}
