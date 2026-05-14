using Dialysis.CQRS.Queries;

namespace Dialysis.HIS.Operations.Features.GetBillingExportJobById;

public sealed record GetBillingExportJobByIdQuery(Guid Id) : IQuery<BillingExportJobStatusDto?>;

public sealed record BillingExportJobStatusDto(
    Guid Id,
    string PayerCode,
    string StatusCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateTime SubmittedAtUtc,
    DateTime? CompletedAtUtc,
    string? Notes);
