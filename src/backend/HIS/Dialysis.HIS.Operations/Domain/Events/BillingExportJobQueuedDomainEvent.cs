using Dialysis.DomainDrivenDesign.DomainEvents;

namespace Dialysis.HIS.Operations.Domain.Events;

/// <summary>
/// In-bounded-context domain event raised when a billing export job is queued. Distinct from the
/// outbound <c>BillingExportJobQueuedIntegrationEvent</c> in <c>HIS.Contracts</c>: this fires within
/// HIS for audit/projection handlers post-commit.
/// </summary>
public sealed record BillingExportJobQueuedDomainEvent(
    Guid JobId,
    string PayerCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateTime QueuedAtUtc) : DomainEvent;
