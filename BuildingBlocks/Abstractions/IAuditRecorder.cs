namespace BuildingBlocks.Abstractions;

/// <summary>
/// Records security-relevant actions for C5 audit compliance.
/// Implementations may log, persist to storage, or publish to an audit service.
/// </summary>
public interface IAuditRecorder
{
    /// <summary>
    /// Records an audit event for a security-relevant action.
    /// </summary>
    /// <param name="request">Audit record details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(AuditRecordRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request data for recording an audit event.
/// Maps to FHIR AuditEvent when using a FHIR-based implementation.
/// </summary>
/// <param name="Action">CRUD action: Create, Read, Update, Delete.</param>
/// <param name="ResourceType">Resource type (e.g. Prescription, Patient, Alarm).</param>
/// <param name="ResourceId">Resource identifier, if applicable.</param>
/// <param name="UserId">User or system that performed the action.</param>
/// <param name="Outcome">0=success, 4=minor failure, 8=serious failure, 12=major failure.</param>
/// <param name="Description">Human-readable description.</param>
/// <param name="TenantId">Tenant for multi-tenant isolation.</param>
public sealed record AuditRecordRequest(
    AuditAction Action,
    string ResourceType,
    string? ResourceId,
    string? UserId,
    AuditOutcome Outcome = AuditOutcome.Success,
    string? Description = null,
    string? TenantId = null);

/// <summary>
/// FHIR AuditEvent action codes.
/// </summary>
public enum AuditAction
{
    Create = 1,  // C
    Read = 2,    // R
    Update = 3,  // U
    Delete = 4,  // D
    Execute = 5
}

/// <summary>
/// FHIR AuditEvent outcome codes.
/// </summary>
public enum AuditOutcome
{
    Success = 0,
    MinorFailure = 4,
    SeriousFailure = 8,
    MajorFailure = 12
}
