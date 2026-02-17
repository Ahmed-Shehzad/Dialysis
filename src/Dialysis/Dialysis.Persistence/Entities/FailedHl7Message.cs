using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Entities;

/// <summary>
/// Dead-letter queue for unprocessable HL7 messages. Phase 4.1.3.
/// </summary>
public sealed class FailedHl7Message
{
    public Ulid Id { get; set; }
    public TenantId TenantId { get; set; } = null!;
    public string RawMessage { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public string? MessageControlId { get; set; }
    public DateTime FailedAtUtc { get; set; }
    public int RetryCount { get; set; }

    public static FailedHl7Message Create(TenantId tenantId, string rawMessage, string errorMessage, string? messageControlId)
    {
        return new FailedHl7Message
        {
            Id = Ulid.NewUlid(),
            TenantId = tenantId,
            RawMessage = rawMessage,
            ErrorMessage = errorMessage,
            MessageControlId = messageControlId,
            FailedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };
    }
}
