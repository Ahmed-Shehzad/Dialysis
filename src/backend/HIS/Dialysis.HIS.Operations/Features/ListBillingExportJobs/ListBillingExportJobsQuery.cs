using Dialysis.CQRS.Queries;

namespace Dialysis.HIS.Operations.Features.ListBillingExportJobs;

/// <summary>
/// Operator-dashboard query: returns recent <c>BillingExportJob</c> rows, optionally filtered
/// to a single status. Drives the <c>/admin/billing/exports</c> SPA page; the
/// <c>OperationsController</c> exposes this on <c>GET /api/v1.0/operations/billing/export-jobs</c>.
/// </summary>
public sealed record ListBillingExportJobsQuery(string? Status, int Take)
    : IQuery<IReadOnlyList<BillingExportJobRow>>;

public sealed record BillingExportJobRow(
    Guid Id,
    string PayerCode,
    string StatusCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateTime SubmittedAtUtc,
    DateTime? CompletedAtUtc,
    string? Notes);
