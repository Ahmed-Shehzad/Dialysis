using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.Billing;

/// <summary>
/// Facility-operations trigger: a billing export job has been queued in HIS. EHR's billing module is the
/// authoritative claims/charges/remittance pipeline — this event hands the period + payer over so EHR can
/// execute the actual export. HIS owns the queue; EHR owns the domain.
/// </summary>
public sealed record BillingExportJobQueuedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Facility-operations trigger: a billing export job has been queued in HIS. EHR's billing module is the
    /// authoritative claims/charges/remittance pipeline — this event hands the period + payer over so EHR can
    /// execute the actual export. HIS owns the queue; EHR owns the domain.
    /// </summary>
    public BillingExportJobQueuedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid JobId,
        string PayerCode,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        string? Notes)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.JobId = JobId;
        this.PayerCode = PayerCode;
        this.PeriodStart = PeriodStart;
        this.PeriodEnd = PeriodEnd;
        this.Notes = Notes;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid JobId { get; init; }
    public string PayerCode { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public string? Notes { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid JobId, out string PayerCode, out DateOnly PeriodStart, out DateOnly PeriodEnd, out string? Notes)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        JobId = this.JobId;
        PayerCode = this.PayerCode;
        PeriodStart = this.PeriodStart;
        PeriodEnd = this.PeriodEnd;
        Notes = this.Notes;
    }
}
