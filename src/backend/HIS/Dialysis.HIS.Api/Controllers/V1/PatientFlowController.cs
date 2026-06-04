using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.PatientFlow.Features.AdmitPatient;
using Dialysis.HIS.PatientFlow.Features.AssignChair;
using Dialysis.HIS.PatientFlow.Features.CheckInPatient;
using Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;
using Dialysis.HIS.PatientFlow.Features.RegisterWalkIn;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patient-flow")]
public sealed class PatientFlowController : HisHateoasControllerBase
{
    private readonly ICqrsGateway _gateway;
    public PatientFlowController(ICqrsGateway gateway) => _gateway = gateway;
    [HttpPost("admissions")]
    [ProducesResponseType(typeof(ResourceEnvelope<AdmitPatientResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AdmitPatientAsync(
        [FromBody] AdmitPatientCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<AdmitPatientCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/patient-flow/admissions/{id}",
            new AdmitPatientResponse(id));
    }

    [HttpGet("todays-queue")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<PatientQueueEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodaysQueueAsync(CancellationToken cancellationToken)
    {
        var entries = await _gateway.SendQueryAsync<GetTodaysQueueQuery, IReadOnlyList<PatientQueueEntryDto>>(
            new GetTodaysQueueQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(entries);
    }

    [HttpPost("queue/check-in")]
    [ProducesResponseType(typeof(ResourceEnvelope<QueueActionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckInAsync(
        [FromBody] CheckInPatientCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<CheckInPatientCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return OkResource(new QueueActionResponse(id));
    }

    [HttpPost("queue/assign-chair")]
    [ProducesResponseType(typeof(ResourceEnvelope<QueueActionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignChairAsync(
        [FromBody] AssignChairCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<AssignChairCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return OkResource(new QueueActionResponse(id));
    }

    [HttpPost("queue/walk-in")]
    [ProducesResponseType(typeof(ResourceEnvelope<PatientQueueEntryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterWalkInAsync(
        [FromBody] RegisterWalkInCommand command,
        CancellationToken cancellationToken)
    {
        var entry = await _gateway.SendCommandAsync<RegisterWalkInCommand, PatientQueueEntryDto>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/patient-flow/queue/{entry.Id}",
            entry);
    }

    public sealed record AdmitPatientResponse
    {
        public AdmitPatientResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = this.Id;
    }

    public sealed record QueueActionResponse
    {
        public QueueActionResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = this.Id;
    }
}
