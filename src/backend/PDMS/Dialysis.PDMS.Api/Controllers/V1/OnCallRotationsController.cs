using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.OnCall.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

/// <summary>
/// On-call rotation surface. Drives the rotations page under <c>/admin/oncall/*</c>.
/// Every action is tagged with <see cref="PhiAccessAttribute"/> so the audit filter emits
/// a FHIR <c>AuditEvent</c> per call — the GDPR / BDSG audit trail covers operator
/// scheduling changes alongside clinical PHI reads.
///
/// Rotation lifecycle:
/// <list type="bullet">
///   <item><c>GET /api/v1.0/oncall/rotations?chairId=&amp;atUtc=</c> — active rotations</item>
///   <item><c>POST /api/v1.0/oncall/rotations</c> — create</item>
///   <item><c>PUT /api/v1.0/oncall/rotations/{id}</c> — replace (operator save)</item>
/// </list>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oncall")]
public sealed class OnCallRotationsController : ControllerBase
{
    private readonly IPdmsRepository<OnCallRotation, Guid> _rotations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OnCallRotationsController> _logger;
    /// <summary>
    /// On-call rotation surface. Drives the rotations page under <c>/admin/oncall/*</c>.
    /// Every action is tagged with <see cref="PhiAccessAttribute"/> so the audit filter emits
    /// a FHIR <c>AuditEvent</c> per call — the GDPR / BDSG audit trail covers operator
    /// scheduling changes alongside clinical PHI reads.
    ///
    /// Rotation lifecycle:
    /// <list type="bullet">
    ///   <item><c>GET /api/v1.0/oncall/rotations?chairId=&amp;atUtc=</c> — active rotations</item>
    ///   <item><c>POST /api/v1.0/oncall/rotations</c> — create</item>
    ///   <item><c>PUT /api/v1.0/oncall/rotations/{id}</c> — replace (operator save)</item>
    /// </list>
    /// </summary>
    public OnCallRotationsController(IPdmsRepository<OnCallRotation, Guid> rotations,
        IUnitOfWork unitOfWork,
        ILogger<OnCallRotationsController> logger)
    {
        _rotations = rotations;
        _unitOfWork = unitOfWork;
        _logger = logger;
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
        try
        { rotation = BuildRotation(Guid.CreateVersion7(), request); }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Rejected on-call rotation create: {Reason}", ex.Message);
            return BadRequest(ex.Message);
        }
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Rejected on-call rotation update for {RotationId}: {Reason}", id, ex.Message);
            return BadRequest(ex.Message);
        }
        _rotations.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(OnCallRotationDto.From(existing));
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
