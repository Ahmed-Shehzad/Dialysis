namespace Dialysis.Module.Contracts.Audit;

/// <summary>
/// Application-level audit log for security-sensitive actions (sign-in, permission grant, sensitive read, etc.).
/// Distinct from the entity-level <c>Audit</c> primitive — that one is stamped automatically by the SaveChanges interceptor.
/// </summary>
public interface IAuditTrail
{
    Task WriteAsync(string actionCode, string? subjectId, string? details, CancellationToken cancellationToken = default);
}
