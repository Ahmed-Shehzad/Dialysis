using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.OnCall.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

/// <summary>
/// Escalation-policy surface. Drives the policies page under <c>/admin/oncall/*</c>.
/// Every action is tagged with <see cref="PhiAccessAttribute"/> so the audit filter emits
/// a FHIR <c>AuditEvent</c> per call — the GDPR / BDSG audit trail covers operator
/// scheduling changes alongside clinical PHI reads.
///
/// Policy lifecycle:
/// <list type="bullet">
///   <item><c>GET /api/v1.0/oncall/policies</c></item>
///   <item><c>POST /api/v1.0/oncall/policies</c></item>
///   <item><c>PUT /api/v1.0/oncall/policies/{id}</c></item>
/// </list>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oncall")]
public sealed class EscalationPoliciesController : ControllerBase
{
    private readonly IPdmsRepository<EscalationPolicy, Guid> _policies;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EscalationPoliciesController> _logger;
    /// <summary>
    /// Escalation-policy surface. Drives the policies page under <c>/admin/oncall/*</c>.
    /// Every action is tagged with <see cref="PhiAccessAttribute"/> so the audit filter emits
    /// a FHIR <c>AuditEvent</c> per call — the GDPR / BDSG audit trail covers operator
    /// scheduling changes alongside clinical PHI reads.
    ///
    /// Policy lifecycle:
    /// <list type="bullet">
    ///   <item><c>GET /api/v1.0/oncall/policies</c></item>
    ///   <item><c>POST /api/v1.0/oncall/policies</c></item>
    ///   <item><c>PUT /api/v1.0/oncall/policies/{id}</c></item>
    /// </list>
    /// </summary>
    public EscalationPoliciesController(IPdmsRepository<EscalationPolicy, Guid> policies,
        IUnitOfWork unitOfWork,
        ILogger<EscalationPoliciesController> logger)
    {
        _policies = policies;
        _unitOfWork = unitOfWork;
        _logger = logger;
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Rejected escalation-policy create: {Reason}", ex.Message);
            return BadRequest(ex.Message);
        }
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Rejected escalation-policy update for {PolicyId}: {Reason}", id, ex.Message);
            return BadRequest(ex.Message);
        }
        _policies.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(EscalationPolicyDto.From(existing));
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
