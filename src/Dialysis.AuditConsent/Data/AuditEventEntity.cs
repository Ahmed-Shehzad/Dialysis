namespace Dialysis.AuditConsent.Data;

public sealed class AuditEventEntity
{
    public required string Id { get; init; }
    public required string TenantId { get; init; }
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public required string Action { get; init; }
    public string? AgentId { get; init; }
    public string? Outcome { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
}
