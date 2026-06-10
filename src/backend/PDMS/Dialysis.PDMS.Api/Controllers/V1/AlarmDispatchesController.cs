using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.OnCall.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

/// <summary>
/// Alarm-dispatch audit surface. Drives the dispatch page under <c>/admin/oncall/*</c>.
/// Every action is tagged with <see cref="PhiAccessAttribute"/> so the audit filter emits
/// a FHIR <c>AuditEvent</c> per call — the GDPR / BDSG audit trail covers operator
/// scheduling changes alongside clinical PHI reads.
///
/// Dispatch audit:
/// <list type="bullet">
///   <item><c>GET /api/v1.0/oncall/dispatches?from=&amp;to=</c></item>
///   <item><c>POST /api/v1.0/oncall/{chairId}/acknowledge</c></item>
/// </list>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oncall")]
public sealed class AlarmDispatchesController : ControllerBase
{
    private readonly IPdmsRepository<AlarmDispatch, Guid> _dispatches;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    /// <summary>
    /// Alarm-dispatch audit surface. Drives the dispatch page under <c>/admin/oncall/*</c>.
    /// Every action is tagged with <see cref="PhiAccessAttribute"/> so the audit filter emits
    /// a FHIR <c>AuditEvent</c> per call — the GDPR / BDSG audit trail covers operator
    /// scheduling changes alongside clinical PHI reads.
    ///
    /// Dispatch audit:
    /// <list type="bullet">
    ///   <item><c>GET /api/v1.0/oncall/dispatches?from=&amp;to=</c></item>
    ///   <item><c>POST /api/v1.0/oncall/{chairId}/acknowledge</c></item>
    /// </list>
    /// </summary>
    public AlarmDispatchesController(IPdmsRepository<AlarmDispatch, Guid> dispatches,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _dispatches = dispatches;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    [HttpGet("dispatches")]
    [PhiAccess("pdms.oncall.dispatches.read")]
    [ProducesResponseType(typeof(IReadOnlyList<AlarmDispatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDispatchesAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? chairId = null,
        CancellationToken cancellationToken = default)
    {
        var all = await _dispatches.ListAsync(null, cancellationToken).ConfigureAwait(false);
        IEnumerable<AlarmDispatch> filtered = all;
        if (from is DateTime f)
            filtered = filtered.Where(d => d.StartedAtUtc >= f);
        if (to is DateTime t)
            filtered = filtered.Where(d => d.StartedAtUtc <= t);
        if (chairId is Guid c)
            filtered = filtered.Where(d => d.ChairId == c);
        return Ok(filtered.OrderByDescending(d => d.StartedAtUtc).Select(AlarmDispatchDto.From).ToArray());
    }

    [HttpPost("{chairId:guid}/acknowledge")]
    [PhiAccess("pdms.oncall.acknowledge", PatientIdRouteKey = "chairId")]
    [ProducesResponseType(typeof(AlarmDispatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcknowledgeAsync(
        Guid chairId,
        [FromBody] AcknowledgeAlarmRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var all = await _dispatches.ListAsync(null, cancellationToken).ConfigureAwait(false);
        var open = all
            .Where(d => d.ChairId == chairId && d.Status != AlarmDispatchStatus.Acknowledged && d.Status != AlarmDispatchStatus.Exhausted)
            .OrderByDescending(d => d.StartedAtUtc)
            .FirstOrDefault();
        if (open is null)
            return NotFound();
        open.Acknowledge(request.ClinicianSub, _clock.GetUtcNow().UtcDateTime);
        _dispatches.Update(open);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(AlarmDispatchDto.From(open));
    }
}

public sealed record AcknowledgeAlarmRequest
{
    public AcknowledgeAlarmRequest(string ClinicianSub) => this.ClinicianSub = ClinicianSub;
    public string ClinicianSub { get; init; }
    public void Deconstruct(out string clinicianSub) => clinicianSub = ClinicianSub;
}

public sealed record AlarmDispatchDto
{
    public AlarmDispatchDto(Guid Id,
        Guid InfusionId,
        Guid SessionId,
        Guid ChairId,
        string AlarmCode,
        string Severity,
        DateTime StartedAtUtc,
        DateTime? ResolvedAtUtc,
        string Status,
        int CurrentLinkIndex,
        string? AcknowledgedBySub,
        IReadOnlyList<AlarmDispatchAttemptDto> Attempts)
    {
        this.Id = Id;
        this.InfusionId = InfusionId;
        this.SessionId = SessionId;
        this.ChairId = ChairId;
        this.AlarmCode = AlarmCode;
        this.Severity = Severity;
        this.StartedAtUtc = StartedAtUtc;
        this.ResolvedAtUtc = ResolvedAtUtc;
        this.Status = Status;
        this.CurrentLinkIndex = CurrentLinkIndex;
        this.AcknowledgedBySub = AcknowledgedBySub;
        this.Attempts = Attempts;
    }
    public static AlarmDispatchDto From(AlarmDispatch d) => new(
        d.Id,
        d.InfusionId,
        d.SessionId,
        d.ChairId,
        d.AlarmCode,
        d.Severity.ToString(),
        d.StartedAtUtc,
        d.ResolvedAtUtc,
        d.Status.ToString(),
        d.CurrentLinkIndex,
        d.AcknowledgedBySub,
        [.. d.Attempts.Select(a => new AlarmDispatchAttemptDto(
            a.ChainLinkIndex,
            a.Channel.ToString(),
            a.Address,
            a.Delivered,
            a.FailureReason,
            a.AttemptedAtUtc))]);
    public Guid Id { get; init; }
    public Guid InfusionId { get; init; }
    public Guid SessionId { get; init; }
    public Guid ChairId { get; init; }
    public string AlarmCode { get; init; }
    public string Severity { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
    public string Status { get; init; }
    public int CurrentLinkIndex { get; init; }
    public string? AcknowledgedBySub { get; init; }
    public IReadOnlyList<AlarmDispatchAttemptDto> Attempts { get; init; }
    public void Deconstruct(out Guid id, out Guid infusionId, out Guid sessionId, out Guid chairId, out string alarmCode, out string severity, out DateTime startedAtUtc, out DateTime? resolvedAtUtc, out string status, out int currentLinkIndex, out string? acknowledgedBySub, out IReadOnlyList<AlarmDispatchAttemptDto> attempts)
    {
        id = Id;
        infusionId = InfusionId;
        sessionId = SessionId;
        chairId = ChairId;
        alarmCode = AlarmCode;
        severity = Severity;
        startedAtUtc = StartedAtUtc;
        resolvedAtUtc = ResolvedAtUtc;
        status = Status;
        currentLinkIndex = CurrentLinkIndex;
        acknowledgedBySub = AcknowledgedBySub;
        attempts = Attempts;
    }
}

public sealed record AlarmDispatchAttemptDto
{
    public AlarmDispatchAttemptDto(int ChainLinkIndex,
        string Channel,
        string Address,
        bool Delivered,
        string? FailureReason,
        DateTime AttemptedAtUtc)
    {
        this.ChainLinkIndex = ChainLinkIndex;
        this.Channel = Channel;
        this.Address = Address;
        this.Delivered = Delivered;
        this.FailureReason = FailureReason;
        this.AttemptedAtUtc = AttemptedAtUtc;
    }
    public int ChainLinkIndex { get; init; }
    public string Channel { get; init; }
    public string Address { get; init; }
    public bool Delivered { get; init; }
    public string? FailureReason { get; init; }
    public DateTime AttemptedAtUtc { get; init; }
    public void Deconstruct(out int chainLinkIndex, out string channel, out string address, out bool delivered, out string? failureReason, out DateTime attemptedAtUtc)
    {
        chainLinkIndex = ChainLinkIndex;
        channel = Channel;
        address = Address;
        delivered = Delivered;
        failureReason = FailureReason;
        attemptedAtUtc = AttemptedAtUtc;
    }
}
