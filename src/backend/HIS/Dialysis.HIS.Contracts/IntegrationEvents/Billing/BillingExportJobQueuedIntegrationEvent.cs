using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.Billing;

/// <summary>
/// Facility-operations trigger: a billing export job has been queued in HIS. EHR's billing module is the
/// authoritative claims/charges/remittance pipeline — this event hands the period + payer over so EHR can
/// execute the actual export. HIS owns the queue; EHR owns the domain.
/// </summary>
public sealed record BillingExportJobQueuedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid JobId,
    string PayerCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? Notes) : IIntegrationEvent;
