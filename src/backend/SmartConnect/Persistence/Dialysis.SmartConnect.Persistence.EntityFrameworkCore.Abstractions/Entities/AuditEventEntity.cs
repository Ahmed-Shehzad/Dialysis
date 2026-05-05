namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class AuditEventEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public int Category { get; set; }

    public int Level { get; set; }

    public Guid? FlowId { get; set; }

    public string? UserId { get; set; }

    public required string Summary { get; set; }

    public string? AttributesJson { get; set; }
}
