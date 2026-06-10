using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.GetBillingExportJobById;

public sealed record GetBillingExportJobByIdQuery : IQuery<BillingExportJobStatusDto?>, IPermissionedCommand
{
    /// <summary>Billing export job status exposes payer codes and periods — gated on the billing/report permission.</summary>
    public string RequiredPermission => HisPermissions.DataReport;
    public GetBillingExportJobByIdQuery(Guid Id) => this.Id = Id;
    public Guid Id { get; init; }
    public void Deconstruct(out Guid id) => id = Id;
}

public sealed record BillingExportJobStatusDto
{
    public BillingExportJobStatusDto(Guid Id,
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
    public void Deconstruct(out Guid id, out string payerCode, out string statusCode, out DateOnly periodStart, out DateOnly periodEnd, out DateTime submittedAtUtc, out DateTime? completedAtUtc, out string? notes)
    {
        id = Id;
        payerCode = PayerCode;
        statusCode = StatusCode;
        periodStart = PeriodStart;
        periodEnd = PeriodEnd;
        submittedAtUtc = SubmittedAtUtc;
        completedAtUtc = CompletedAtUtc;
        notes = Notes;
    }
}
