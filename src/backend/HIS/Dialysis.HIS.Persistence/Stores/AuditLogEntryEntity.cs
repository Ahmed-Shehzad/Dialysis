namespace Dialysis.HIS.Persistence.Stores;

public sealed class AuditLogEntryEntity
{
    public Guid Id { get; set; }

    public string ActionCode { get; set; } = string.Empty;

    public string? SubjectId { get; set; }

    public string? Details { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? ActorUserId { get; set; }
}
