using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.DomainDrivenDesign.Persistence;
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
public sealed class OnCallController : ControllerBase
{
    private readonly IPdmsRepository<OnCallRotation, Guid> _rotations;
    private readonly IPdmsRepository<EscalationPolicy, Guid> _policies;
    private readonly IPdmsRepository<AlarmDispatch, Guid> _dispatches;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
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
    public OnCallController(IPdmsRepository<OnCallRotation, Guid> rotations,
        IPdmsRepository<EscalationPolicy, Guid> policies,
        IPdmsRepository<AlarmDispatch, Guid> dispatches,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _rotations = rotations;
        _policies = policies;
        _dispatches = dispatches;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    [HttpGet("rotations")]
    [PhiAccess("pdms.oncall.rotations.read")]
    [ProducesResponseType(typeof(IReadOnlyList<OnCallRotationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRotationsAsync(
        [FromQuery] Guid? chairId = null,
        [FromQuery] DateTime? atUtc = null,
        CancellationToken cancellationToken = default)
    {
        var all = await _rotations.ListAsync(null, cancellationToken).ConfigureAwait(false);
        IEnumerable<OnCallRotation> filtered = all;
        if (chairId is not null)
            filtered = filtered.Where(r => r.ChairId == chairId.Value);
        if (atUtc is DateTime instant)
            filtered = filtered.Where(r => r.CoversInstant(instant));
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
        OnCallRotation rotation;
        try { rotation = BuildRotation(Guid.CreateVersion7(), request); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        await _rotations.AddAsync(rotation, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        // Literal Location URI (not CreatedAtAction): URL-segment API versioning can't resolve the
        // {version} route value for action-link generation, which throws -> 500.
        return Created($"/api/v1.0/oncall/rotations/{rotation.Id}", OnCallRotationDto.From(rotation));
    }

    [HttpPut("rotations/{id:guid}")]
    [PhiAccess("pdms.oncall.rotations.update")]
    [ProducesResponseType(typeof(OnCallRotationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceRotationAsync(
        Guid id,
        [FromBody] UpsertRotationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _rotations.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return NotFound();
        // Mutate the tracked aggregate in place: remove+re-add with the same key throws an
        // EF identity conflict at SaveChanges (the deleted instance stays tracked by Id). The
        // shift/chain are jsonb columns, so this is a single UPDATE on the existing row.
        try
        {
            existing.Reassign(
                request.ChairId,
                BuildShift(request.ShiftCode),
                request.EffectiveFromUtc,
                request.EffectiveUntilUtc,
                BuildLink(request.Primary),
                BuildLink(request.Backup),
                BuildLink(request.Supervisor));
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        _rotations.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(OnCallRotationDto.From(existing));
    }

    [HttpGet("policies")]
    [PhiAccess("pdms.oncall.policies.read")]
    [ProducesResponseType(typeof(IReadOnlyList<EscalationPolicyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPoliciesAsync(CancellationToken cancellationToken)
    {
        var all = await _policies.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return Ok(all.Select(EscalationPolicyDto.From).ToArray());
    }

    [HttpPost("policies")]
    [PhiAccess("pdms.oncall.policies.create")]
    [ProducesResponseType(typeof(EscalationPolicyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePolicyAsync(
        [FromBody] UpsertPolicyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        EscalationPolicy policy;
        try
        {
            policy = new EscalationPolicy(
                Guid.CreateVersion7(),
                request.Name,
                TimeSpan.FromSeconds(request.CriticalPrimaryWindowSeconds),
                TimeSpan.FromSeconds(request.CriticalBackupWindowSeconds),
                TimeSpan.FromSeconds(request.WarningPrimaryWindowSeconds),
                TimeSpan.FromSeconds(request.WarningBackupWindowSeconds),
                TimeSpan.FromSeconds(request.InformationalPrimaryWindowSeconds),
                request.QuietHoursSuppressNonCritical);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        await _policies.AddAsync(policy, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        // Literal Location URI (not CreatedAtAction): URL-segment API versioning can't resolve the
        // {version} route value for action-link generation, which throws -> 500.
        return Created($"/api/v1.0/oncall/policies/{policy.Id}", EscalationPolicyDto.From(policy));
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
        var existing = await _policies.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return NotFound();
        // Mutate the tracked aggregate in place: remove+re-add with the same key throws an
        // EF identity conflict at SaveChanges (the deleted instance stays tracked by Id).
        try
        {
            existing.Reconfigure(
                request.Name,
                TimeSpan.FromSeconds(request.CriticalPrimaryWindowSeconds),
                TimeSpan.FromSeconds(request.CriticalBackupWindowSeconds),
                TimeSpan.FromSeconds(request.WarningPrimaryWindowSeconds),
                TimeSpan.FromSeconds(request.WarningBackupWindowSeconds),
                TimeSpan.FromSeconds(request.InformationalPrimaryWindowSeconds),
                request.QuietHoursSuppressNonCritical);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        _policies.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(EscalationPolicyDto.From(existing));
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

    private static OnCallRotation BuildRotation(Guid id, UpsertRotationRequest request) =>
        new(
            id,
            request.ChairId,
            BuildShift(request.ShiftCode),
            request.EffectiveFromUtc,
            request.EffectiveUntilUtc,
            BuildLink(request.Primary),
            BuildLink(request.Backup),
            BuildLink(request.Supervisor));

    private static OnCallShift BuildShift(string shiftCode) => shiftCode switch
    {
        "morning" => OnCallShift.Morning,
        "afternoon" => OnCallShift.Afternoon,
        "night" => OnCallShift.Night,
        _ => throw new ArgumentException($"Unknown shift code '{shiftCode}'."),
    };

    private static OnCallChainLink BuildLink(ChainLinkRequest req) =>
        new(req.ClinicianSub, req.DisplayName, [.. req.Channels.Select(c => new NotificationChannelTarget(ParseChannel(c.Channel), c.Address))]);

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

public sealed record UpsertRotationRequest
{
    public UpsertRotationRequest(Guid ChairId,
        string ShiftCode,
        DateOnly EffectiveFromUtc,
        DateOnly EffectiveUntilUtc,
        ChainLinkRequest Primary,
        ChainLinkRequest Backup,
        ChainLinkRequest Supervisor)
    {
        this.ChairId = ChairId;
        this.ShiftCode = ShiftCode;
        this.EffectiveFromUtc = EffectiveFromUtc;
        this.EffectiveUntilUtc = EffectiveUntilUtc;
        this.Primary = Primary;
        this.Backup = Backup;
        this.Supervisor = Supervisor;
    }
    public Guid ChairId { get; init; }
    public string ShiftCode { get; init; }
    public DateOnly EffectiveFromUtc { get; init; }
    public DateOnly EffectiveUntilUtc { get; init; }
    public ChainLinkRequest Primary { get; init; }
    public ChainLinkRequest Backup { get; init; }
    public ChainLinkRequest Supervisor { get; init; }
    public void Deconstruct(out Guid chairId, out string shiftCode, out DateOnly effectiveFromUtc, out DateOnly effectiveUntilUtc, out ChainLinkRequest primary, out ChainLinkRequest backup, out ChainLinkRequest supervisor)
    {
        chairId = ChairId;
        shiftCode = ShiftCode;
        effectiveFromUtc = EffectiveFromUtc;
        effectiveUntilUtc = EffectiveUntilUtc;
        primary = Primary;
        backup = Backup;
        supervisor = Supervisor;
    }
}

public sealed record ChainLinkRequest
{
    public ChainLinkRequest(string ClinicianSub,
        string DisplayName,
        IReadOnlyList<ChannelTargetRequest> Channels)
    {
        this.ClinicianSub = ClinicianSub;
        this.DisplayName = DisplayName;
        this.Channels = Channels;
    }
    public string ClinicianSub { get; init; }
    public string DisplayName { get; init; }
    public IReadOnlyList<ChannelTargetRequest> Channels { get; init; }
    public void Deconstruct(out string clinicianSub, out string displayName, out IReadOnlyList<ChannelTargetRequest> channels)
    {
        clinicianSub = ClinicianSub;
        displayName = DisplayName;
        channels = Channels;
    }
}

public sealed record ChannelTargetRequest
{
    public ChannelTargetRequest(string Channel, string Address)
    {
        this.Channel = Channel;
        this.Address = Address;
    }
    public string Channel { get; init; }
    public string Address { get; init; }
    public void Deconstruct(out string channel, out string address)
    {
        channel = Channel;
        address = Address;
    }
}

public sealed record UpsertPolicyRequest
{
    public UpsertPolicyRequest(string Name,
        int CriticalPrimaryWindowSeconds,
        int CriticalBackupWindowSeconds,
        int WarningPrimaryWindowSeconds,
        int WarningBackupWindowSeconds,
        int InformationalPrimaryWindowSeconds,
        bool QuietHoursSuppressNonCritical)
    {
        this.Name = Name;
        this.CriticalPrimaryWindowSeconds = CriticalPrimaryWindowSeconds;
        this.CriticalBackupWindowSeconds = CriticalBackupWindowSeconds;
        this.WarningPrimaryWindowSeconds = WarningPrimaryWindowSeconds;
        this.WarningBackupWindowSeconds = WarningBackupWindowSeconds;
        this.InformationalPrimaryWindowSeconds = InformationalPrimaryWindowSeconds;
        this.QuietHoursSuppressNonCritical = QuietHoursSuppressNonCritical;
    }
    public string Name { get; init; }
    public int CriticalPrimaryWindowSeconds { get; init; }
    public int CriticalBackupWindowSeconds { get; init; }
    public int WarningPrimaryWindowSeconds { get; init; }
    public int WarningBackupWindowSeconds { get; init; }
    public int InformationalPrimaryWindowSeconds { get; init; }
    public bool QuietHoursSuppressNonCritical { get; init; }
    public void Deconstruct(out string name, out int criticalPrimaryWindowSeconds, out int criticalBackupWindowSeconds, out int warningPrimaryWindowSeconds, out int warningBackupWindowSeconds, out int informationalPrimaryWindowSeconds, out bool quietHoursSuppressNonCritical)
    {
        name = Name;
        criticalPrimaryWindowSeconds = CriticalPrimaryWindowSeconds;
        criticalBackupWindowSeconds = CriticalBackupWindowSeconds;
        warningPrimaryWindowSeconds = WarningPrimaryWindowSeconds;
        warningBackupWindowSeconds = WarningBackupWindowSeconds;
        informationalPrimaryWindowSeconds = InformationalPrimaryWindowSeconds;
        quietHoursSuppressNonCritical = QuietHoursSuppressNonCritical;
    }
}

public sealed record AcknowledgeAlarmRequest
{
    public AcknowledgeAlarmRequest(string ClinicianSub) => this.ClinicianSub = ClinicianSub;
    public string ClinicianSub { get; init; }
    public void Deconstruct(out string clinicianSub) => clinicianSub = ClinicianSub;
}

public sealed record OnCallRotationDto
{
    public OnCallRotationDto(Guid Id,
        Guid ChairId,
        string ShiftCode,
        DateOnly EffectiveFromUtc,
        DateOnly EffectiveUntilUtc,
        ChainLinkDto Primary,
        ChainLinkDto Backup,
        ChainLinkDto Supervisor)
    {
        this.Id = Id;
        this.ChairId = ChairId;
        this.ShiftCode = ShiftCode;
        this.EffectiveFromUtc = EffectiveFromUtc;
        this.EffectiveUntilUtc = EffectiveUntilUtc;
        this.Primary = Primary;
        this.Backup = Backup;
        this.Supervisor = Supervisor;
    }
    public static OnCallRotationDto From(OnCallRotation r) => new(
        r.Id,
        r.ChairId,
        r.Shift.Code,
        r.EffectiveFromUtc,
        r.EffectiveUntilUtc,
        ChainLinkDto.From(r.Primary),
        ChainLinkDto.From(r.Backup),
        ChainLinkDto.From(r.Supervisor));
    public Guid Id { get; init; }
    public Guid ChairId { get; init; }
    public string ShiftCode { get; init; }
    public DateOnly EffectiveFromUtc { get; init; }
    public DateOnly EffectiveUntilUtc { get; init; }
    public ChainLinkDto Primary { get; init; }
    public ChainLinkDto Backup { get; init; }
    public ChainLinkDto Supervisor { get; init; }
    public void Deconstruct(out Guid id, out Guid chairId, out string shiftCode, out DateOnly effectiveFromUtc, out DateOnly effectiveUntilUtc, out ChainLinkDto primary, out ChainLinkDto backup, out ChainLinkDto supervisor)
    {
        id = Id;
        chairId = ChairId;
        shiftCode = ShiftCode;
        effectiveFromUtc = EffectiveFromUtc;
        effectiveUntilUtc = EffectiveUntilUtc;
        primary = Primary;
        backup = Backup;
        supervisor = Supervisor;
    }
}

public sealed record ChainLinkDto
{
    public ChainLinkDto(string ClinicianSub, string DisplayName, IReadOnlyList<ChannelTargetDto> Channels)
    {
        this.ClinicianSub = ClinicianSub;
        this.DisplayName = DisplayName;
        this.Channels = Channels;
    }
    public static ChainLinkDto From(OnCallChainLink l) => new(
        l.ClinicianSub,
        l.DisplayName,
        [.. l.Channels.Select(c => new ChannelTargetDto(c.Channel.ToString(), c.Address))]);
    public string ClinicianSub { get; init; }
    public string DisplayName { get; init; }
    public IReadOnlyList<ChannelTargetDto> Channels { get; init; }
    public void Deconstruct(out string clinicianSub, out string displayName, out IReadOnlyList<ChannelTargetDto> channels)
    {
        clinicianSub = ClinicianSub;
        displayName = DisplayName;
        channels = Channels;
    }
}

public sealed record ChannelTargetDto
{
    public ChannelTargetDto(string Channel, string Address)
    {
        this.Channel = Channel;
        this.Address = Address;
    }
    public string Channel { get; init; }
    public string Address { get; init; }
    public void Deconstruct(out string channel, out string address)
    {
        channel = Channel;
        address = Address;
    }
}

public sealed record EscalationPolicyDto
{
    public EscalationPolicyDto(Guid Id,
        string Name,
        int CriticalPrimaryWindowSeconds,
        int CriticalBackupWindowSeconds,
        int WarningPrimaryWindowSeconds,
        int WarningBackupWindowSeconds,
        int InformationalPrimaryWindowSeconds,
        bool QuietHoursSuppressNonCritical)
    {
        this.Id = Id;
        this.Name = Name;
        this.CriticalPrimaryWindowSeconds = CriticalPrimaryWindowSeconds;
        this.CriticalBackupWindowSeconds = CriticalBackupWindowSeconds;
        this.WarningPrimaryWindowSeconds = WarningPrimaryWindowSeconds;
        this.WarningBackupWindowSeconds = WarningBackupWindowSeconds;
        this.InformationalPrimaryWindowSeconds = InformationalPrimaryWindowSeconds;
        this.QuietHoursSuppressNonCritical = QuietHoursSuppressNonCritical;
    }
    public static EscalationPolicyDto From(EscalationPolicy p) => new(
        p.Id,
        p.Name,
        (int)p.CriticalPrimaryWindow.TotalSeconds,
        (int)p.CriticalBackupWindow.TotalSeconds,
        (int)p.WarningPrimaryWindow.TotalSeconds,
        (int)p.WarningBackupWindow.TotalSeconds,
        (int)p.InformationalPrimaryWindow.TotalSeconds,
        p.QuietHoursSuppressNonCritical);
    public Guid Id { get; init; }
    public string Name { get; init; }
    public int CriticalPrimaryWindowSeconds { get; init; }
    public int CriticalBackupWindowSeconds { get; init; }
    public int WarningPrimaryWindowSeconds { get; init; }
    public int WarningBackupWindowSeconds { get; init; }
    public int InformationalPrimaryWindowSeconds { get; init; }
    public bool QuietHoursSuppressNonCritical { get; init; }
    public void Deconstruct(out Guid id, out string name, out int criticalPrimaryWindowSeconds, out int criticalBackupWindowSeconds, out int warningPrimaryWindowSeconds, out int warningBackupWindowSeconds, out int informationalPrimaryWindowSeconds, out bool quietHoursSuppressNonCritical)
    {
        id = Id;
        name = Name;
        criticalPrimaryWindowSeconds = CriticalPrimaryWindowSeconds;
        criticalBackupWindowSeconds = CriticalBackupWindowSeconds;
        warningPrimaryWindowSeconds = WarningPrimaryWindowSeconds;
        warningBackupWindowSeconds = WarningBackupWindowSeconds;
        informationalPrimaryWindowSeconds = InformationalPrimaryWindowSeconds;
        quietHoursSuppressNonCritical = QuietHoursSuppressNonCritical;
    }
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
