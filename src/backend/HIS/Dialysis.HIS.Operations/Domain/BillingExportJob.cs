namespace Dialysis.HIS.Operations.Domain;

/// <summary>
/// Facility-operations trigger for a payer-billing export window. HIS owns only the queue surface;
/// the actual claim filing lives in <c>Dialysis.EHR.Billing</c> and consumes
/// <c>BillingExportJobQueuedIntegrationEvent</c>.
/// </summary>
public sealed class BillingExportJob
{
    public Guid Id { get; set; }

    public string PayerCode { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public DateTime SubmittedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? Notes { get; set; }
}
