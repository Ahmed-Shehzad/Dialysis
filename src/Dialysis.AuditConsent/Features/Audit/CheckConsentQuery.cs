using Intercessor.Abstractions;

namespace Dialysis.AuditConsent.Features.Audit;

public sealed record CheckConsentQuery : IQuery<CheckConsentResult>
{
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public string Action { get; init; } = "consent-granted";
}

public sealed record CheckConsentResult(bool HasConsent);
