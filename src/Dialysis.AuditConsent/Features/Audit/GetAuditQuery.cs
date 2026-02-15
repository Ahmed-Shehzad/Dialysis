using Intercessor.Abstractions;

namespace Dialysis.AuditConsent.Features.Audit;

public sealed record GetAuditQuery : IQuery<GetAuditResult>
{
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
}

public sealed record GetAuditResult
{
    public required IReadOnlyList<AuditEntryDto> Entries { get; init; }
}
