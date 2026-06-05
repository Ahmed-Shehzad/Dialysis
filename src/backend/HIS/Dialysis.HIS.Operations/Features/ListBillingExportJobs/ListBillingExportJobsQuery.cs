using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.ListBillingExportJobs;

/// <summary>
/// Operator-dashboard query: returns recent <c>BillingExportJob</c> rows, optionally filtered
/// to a single status. Drives the <c>/admin/billing/exports</c> SPA page; the
/// <c>OperationsController</c> exposes this on <c>GET /api/v1.0/operations/billing/export-jobs</c>.
/// </summary>
public sealed record ListBillingExportJobsQuery : IQuery<IReadOnlyList<BillingExportJobRow>>, IPermissionedCommand
{
    /// <summary>Billing export job rows expose payer codes and periods — gated on the billing/report permission.</summary>
    public string RequiredPermission => HisPermissions.DataReport;

    /// <summary>
    /// Operator-dashboard query: returns recent <c>BillingExportJob</c> rows, optionally filtered
    /// to a single status. Drives the <c>/admin/billing/exports</c> SPA page; the
    /// <c>OperationsController</c> exposes this on <c>GET /api/v1.0/operations/billing/export-jobs</c>.
    /// </summary>
    public ListBillingExportJobsQuery(string? Status, int Take)
    {
        this.Status = Status;
        this.Take = Take;
    }
    public string? Status { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out string? Status, out int Take)
    {
        Status = this.Status;
        Take = this.Take;
    }
}

public sealed record BillingExportJobRow
{
    public BillingExportJobRow(Guid Id,
        string PayerCode,
        string StatusCode,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        DateTime SubmittedAtUtc,
        DateTime? CompletedAtUtc,
        string? Notes)
    {
        this.Id = Id;
        this.PayerCode = PayerCode;
        this.StatusCode = StatusCode;
        this.PeriodStart = PeriodStart;
        this.PeriodEnd = PeriodEnd;
        this.SubmittedAtUtc = SubmittedAtUtc;
        this.CompletedAtUtc = CompletedAtUtc;
        this.Notes = Notes;
    }
    public Guid Id { get; init; }
    public string PayerCode { get; init; }
    public string StatusCode { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? Notes { get; init; }
    public void Deconstruct(out Guid Id, out string PayerCode, out string StatusCode, out DateOnly PeriodStart, out DateOnly PeriodEnd, out DateTime SubmittedAtUtc, out DateTime? CompletedAtUtc, out string? Notes)
    {
        Id = this.Id;
        PayerCode = this.PayerCode;
        StatusCode = this.StatusCode;
        PeriodStart = this.PeriodStart;
        PeriodEnd = this.PeriodEnd;
        SubmittedAtUtc = this.SubmittedAtUtc;
        CompletedAtUtc = this.CompletedAtUtc;
        Notes = this.Notes;
    }
}
