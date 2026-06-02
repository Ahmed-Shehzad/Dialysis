using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.OnCall.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

/// <summary>
/// On-call rotation + escalation-policy + alarm-dispatch audit surface. Drives the three
/// <c>/admin/oncall/*</c> SPA pages. Every action is tagged with <see cref="PhiAccessAttribute"/>
/// so the audit filter emits a FHIR <c>AuditEvent</c> per call — the GDPR / BDSG audit
/// trail covers operator scheduling changes alongside clinical PHI reads.
///
/// Rotation lifecycle:
/// <list type="bullet">
///   <item><c>GET /api/v1.0/oncall/rotations?chairId=&amp;atUtc=</c> — active rotations</item>
///   <item><c>POST /api/v1.0/oncall/rotations</c> — create</item>
///   <item><c>PUT /api/v1.0/oncall/rotations/{id}</c> — replace (operator save)</item>
/// </list>
/// Policy lifecycle:
/// <list type="bullet">
///   <item><c>GET /api/v1.0/oncall/policies</c></item>
///   <item><c>PUT /api/v1.0/oncall/policies/{id}</c></item>
/// </list>
/// Dispatch audit:
/// <list type="bullet">
///   <item><c>GET /api/v1.0/oncall/dispatches?from=&amp;to=</c></item>
///   <item><c>POST /api/v1.0/oncall/{chairId}/acknowledge</c></item>
/// </list>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oncall")]
public sealed class OnCallController(
    IPdmsRepository<OnCallRotation, Guid> rotations,
    IPdmsRepository<EscalationPolicy, Guid> policies,
    IPdmsRepository<AlarmDispatch, Guid> dispatches,
    TimeProvider clock) : ControllerBase
{
    [HttpGet("rotations")]
    [PhiAccess("pdms.oncall.rotations.read")]
    [ProducesResponseType(typeof(IReadOnlyList<OnCallRotationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRotationsAsync(
        [FromQuery] Guid? chairId = null,
        [FromQuery] DateTime? atUtc = null,
        CancellationToken cancellationToken = default)
    {
        var all = await rotations.ListAsync(null, cancellationToken).ConfigureAwait(false);
        IEnumerable<OnCallRotation> filtered = all;
        if (chairId is not null) filtered = filtered.Where(r => r.ChairId == chairId.Value);
        if (atUtc is DateTime instant) filtered = filtered.Where(r => r.CoversInstant(instant));
        return Ok(filtered.Select(OnCallRotationDto.From).ToArray());
    }

    [HttpPost("rotations")]
    [PhiAccess("pdms.oncall.rotations.create")]
    [ProducesResponseType(typeof(OnCallRotationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRotationAsync(
        [FromBody] UpsertRotationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rotation = BuildRotation(Guid.CreateVersion7(), request);
        await rotations.AddAsync(rotation, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(ListRotationsAsync), null, OnCallRotationDto.From(rotation));
    }

    [HttpPut("rotations/{id:guid}")]
    [PhiAccess("pdms.oncall.rotations.update")]
    [ProducesResponseType(typeof(OnCallRotationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceRotationAsync(
        Guid id,
        [FromBody] UpsertRotationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await rotations.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null) return NotFound();
        // OnCallRotation is immutable post-create; replacement = remove + re-add with same id.
        rotations.Remove(existing);
        var replacement = BuildRotation(id, request);
        await rotations.AddAsync(replacement, cancellationToken).ConfigureAwait(false);
        return Ok(OnCallRotationDto.From(replacement));
    }

    [HttpGet("policies")]
    [PhiAccess("pdms.oncall.policies.read")]
    [ProducesResponseType(typeof(IReadOnlyList<EscalationPolicyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPoliciesAsync(CancellationToken cancellationToken)
    {
        var all = await policies.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return Ok(all.Select(EscalationPolicyDto.From).ToArray());
    }

    [HttpPut("policies/{id:guid}")]
    [PhiAccess("pdms.oncall.policies.update")]
    [ProducesResponseType(typeof(EscalationPolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplacePolicyAsync(
        Guid id,
        [FromBody] UpsertPolicyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await policies.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null) return NotFound();
        policies.Remove(existing);
        EscalationPolicy replacement;
        try
        {
            replacement = new EscalationPolicy(
                id,
                request.Name,
                TimeSpan.FromSeconds(request.CriticalPrimaryWindowSeconds),
                TimeSpan.FromSeconds(request.CriticalBackupWindowSeconds),
                TimeSpan.FromSeconds(request.WarningPrimaryWindowSeconds),
                TimeSpan.FromSeconds(request.WarningBackupWindowSeconds),
                TimeSpan.FromSeconds(request.InformationalPrimaryWindowSeconds),
                request.QuietHoursSuppressNonCritical);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        await policies.AddAsync(replacement, cancellationToken).ConfigureAwait(false);
        return Ok(EscalationPolicyDto.From(replacement));
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
        var all = await dispatches.ListAsync(null, cancellationToken).ConfigureAwait(false);
        IEnumerable<AlarmDispatch> filtered = all;
        if (from is DateTime f) filtered = filtered.Where(d => d.StartedAtUtc >= f);
        if (to is DateTime t) filtered = filtered.Where(d => d.StartedAtUtc <= t);
        if (chairId is Guid c) filtered = filtered.Where(d => d.ChairId == c);
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
        var all = await dispatches.ListAsync(null, cancellationToken).ConfigureAwait(false);
        var open = all
            .Where(d => d.ChairId == chairId && d.Status != AlarmDispatchStatus.Acknowledged && d.Status != AlarmDispatchStatus.Exhausted)
            .OrderByDescending(d => d.StartedAtUtc)
            .FirstOrDefault();
        if (open is null) return NotFound();
        open.Acknowledge(request.ClinicianSub, clock.GetUtcNow().UtcDateTime);
        dispatches.Update(open);
        return Ok(AlarmDispatchDto.From(open));
    }

    private static OnCallRotation BuildRotation(Guid id, UpsertRotationRequest request)
    {
        var shift = request.ShiftCode switch
        {
            "morning" => OnCallShift.Morning,
            "afternoon" => OnCallShift.Afternoon,
            "night" => OnCallShift.Night,
            _ => throw new ArgumentException($"Unknown shift code '{request.ShiftCode}'."),
        };
        return new OnCallRotation(
            id,
            request.ChairId,
            shift,
            request.EffectiveFromUtc,
            request.EffectiveUntilUtc,
            BuildLink(request.Primary),
            BuildLink(request.Backup),
            BuildLink(request.Supervisor));
    }

    private static OnCallChainLink BuildLink(ChainLinkRequest req) =>
        new(req.ClinicianSub, req.DisplayName, req.Channels
            .Select(c => new NotificationChannelTarget(ParseChannel(c.Channel), c.Address))
            .ToArray());

    private static NotificationChannel ParseChannel(string channel) => channel.ToLowerInvariant() switch
    {
        "sms" => NotificationChannel.Sms,
        "push.apns" => NotificationChannel.PushApns,
        "push.fcm" => NotificationChannel.PushFcm,
        "email" => NotificationChannel.Email,
        "voice" => NotificationChannel.Voice,
        _ => throw new ArgumentException($"Unknown channel '{channel}'."),
    };
}

public sealed record UpsertRotationRequest(
    Guid ChairId,
    string ShiftCode,
    DateOnly EffectiveFromUtc,
    DateOnly EffectiveUntilUtc,
    ChainLinkRequest Primary,
    ChainLinkRequest Backup,
    ChainLinkRequest Supervisor);

public sealed record ChainLinkRequest(
    string ClinicianSub,
    string DisplayName,
    IReadOnlyList<ChannelTargetRequest> Channels);

public sealed record ChannelTargetRequest(string Channel, string Address);

public sealed record UpsertPolicyRequest(
    string Name,
    int CriticalPrimaryWindowSeconds,
    int CriticalBackupWindowSeconds,
    int WarningPrimaryWindowSeconds,
    int WarningBackupWindowSeconds,
    int InformationalPrimaryWindowSeconds,
    bool QuietHoursSuppressNonCritical);

public sealed record AcknowledgeAlarmRequest(string ClinicianSub);

public sealed record OnCallRotationDto(
    Guid Id,
    Guid ChairId,
    string ShiftCode,
    DateOnly EffectiveFromUtc,
    DateOnly EffectiveUntilUtc,
    ChainLinkDto Primary,
    ChainLinkDto Backup,
    ChainLinkDto Supervisor)
{
    public static OnCallRotationDto From(OnCallRotation r) => new(
        r.Id,
        r.ChairId,
        r.Shift.Code,
        r.EffectiveFromUtc,
        r.EffectiveUntilUtc,
        ChainLinkDto.From(r.Primary),
        ChainLinkDto.From(r.Backup),
        ChainLinkDto.From(r.Supervisor));
}

public sealed record ChainLinkDto(string ClinicianSub, string DisplayName, IReadOnlyList<ChannelTargetDto> Channels)
{
    public static ChainLinkDto From(OnCallChainLink l) => new(
        l.ClinicianSub,
        l.DisplayName,
        l.Channels.Select(c => new ChannelTargetDto(c.Channel.ToString(), c.Address)).ToArray());
}

public sealed record ChannelTargetDto(string Channel, string Address);

public sealed record EscalationPolicyDto(
    Guid Id,
    string Name,
    int CriticalPrimaryWindowSeconds,
    int CriticalBackupWindowSeconds,
    int WarningPrimaryWindowSeconds,
    int WarningBackupWindowSeconds,
    int InformationalPrimaryWindowSeconds,
    bool QuietHoursSuppressNonCritical)
{
    public static EscalationPolicyDto From(EscalationPolicy p) => new(
        p.Id,
        p.Name,
        (int)p.CriticalPrimaryWindow.TotalSeconds,
        (int)p.CriticalBackupWindow.TotalSeconds,
        (int)p.WarningPrimaryWindow.TotalSeconds,
        (int)p.WarningBackupWindow.TotalSeconds,
        (int)p.InformationalPrimaryWindow.TotalSeconds,
        p.QuietHoursSuppressNonCritical);
}

public sealed record AlarmDispatchDto(
    Guid Id,
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
        d.Attempts.Select(a => new AlarmDispatchAttemptDto(
            a.ChainLinkIndex,
            a.Channel.ToString(),
            a.Address,
            a.Delivered,
            a.FailureReason,
            a.AttemptedAtUtc)).ToArray());
}

public sealed record AlarmDispatchAttemptDto(
    int ChainLinkIndex,
    string Channel,
    string Address,
    bool Delivered,
    string? FailureReason,
    DateTime AttemptedAtUtc);
