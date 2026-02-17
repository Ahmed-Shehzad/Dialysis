using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Audit;

public sealed record QueryAuditQuery(
    string? PatientId,
    string? ResourceType,
    string? Action,
    DateTime? From,
    DateTime? To,
    int Limit,
    int Offset) : IQuery<IReadOnlyList<AuditEventDto>>;
