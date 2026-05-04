namespace Dialysis.HIS.Operations.Domain;

public sealed class BillingExportJob
{
    public Guid Id { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public string FormatCode { get; set; } = "FHIR_BUNDLE_STUB";

    public string StatusCode { get; set; } = "Queued";

    /// <summary>Optional payer or reimbursement program code for export routing.</summary>
    public string? PayerCode { get; set; }
}
