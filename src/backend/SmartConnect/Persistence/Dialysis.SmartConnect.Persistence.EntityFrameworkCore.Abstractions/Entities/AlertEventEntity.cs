namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class AlertEventEntity
{
    public Guid Id { get; set; }

    public Guid RuleId { get; set; }

    public Guid? FlowId { get; set; }

    public Guid? MessageId { get; set; }

    public string? CorrelationId { get; set; }

    public int ErrorType { get; set; }

    public string? ErrorDetail { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>JSON array of <c>AlertActionOutcome</c>.</summary>
    public string ActionOutcomesJson { get; set; } = "[]";
}
