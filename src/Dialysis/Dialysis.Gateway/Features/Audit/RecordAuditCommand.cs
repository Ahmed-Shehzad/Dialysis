using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Audit;

public sealed record RecordAuditCommand(
    string Action,
    string ResourceType,
    string? Actor,
    string? ResourceId,
    string? PatientId,
    string? Details) : ICommand<RecordAuditResult>;

public sealed record RecordAuditResult(AuditEventDto? Dto, string? Error);
