namespace Dialysis.HIS.Security.Audit;

public interface IAuditTrail
{
    Task WriteAsync(string actionCode, string? subjectId, string? details, CancellationToken cancellationToken = default);
}
