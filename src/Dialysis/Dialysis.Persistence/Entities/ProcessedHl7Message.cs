using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Entities;

/// <summary>
/// Tracks processed HL7 messages by MSH-10 (Message Control ID) for idempotency. Phase 4.1.3.
/// </summary>
public sealed class ProcessedHl7Message
{
    public TenantId TenantId { get; set; } = null!;
    public string MessageControlId { get; set; } = "";
    public DateTime ProcessedAtUtc { get; set; }

    public static ProcessedHl7Message Create(TenantId tenantId, string messageControlId)
    {
        return new ProcessedHl7Message
        {
            TenantId = tenantId,
            MessageControlId = messageControlId,
            ProcessedAtUtc = DateTime.UtcNow
        };
    }
}
