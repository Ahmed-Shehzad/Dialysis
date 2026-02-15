using Intercessor.Abstractions;

namespace Dialysis.AuditConsent.Features.Audit;

public sealed record RecordAuditCommand : ICommand
{
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public required string Action { get; init; }
    public string? AgentId { get; init; }
    public string? Outcome { get; init; }
}
