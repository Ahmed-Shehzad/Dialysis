using Dialysis.DomainDrivenDesign.DomainServices;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.Operations.Domain.Enumerations;
using Dialysis.HIS.Operations.Domain.ValueObjects;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Domain.Services;

/// <summary>
/// Domain service: rejects a new export-job submission when an open (Queued) job already exists for the
/// same payer + reporting period. Coordinates across the repository because the rule sits between aggregates.
/// </summary>
public sealed class BillingExportEligibilityService : IDomainService
{
    private readonly IBillingExportJobRepository _repository;
    /// <summary>
    /// Domain service: rejects a new export-job submission when an open (Queued) job already exists for the
    /// same payer + reporting period. Coordinates across the repository because the rule sits between aggregates.
    /// </summary>
    public BillingExportEligibilityService(IBillingExportJobRepository repository) => _repository = repository;
    public async Task EnsureNoQueuedDuplicateAsync(
        PayerCode payer,
        BillingPeriod period,
        CancellationToken cancellationToken = default)
    {
        var queued = await _repository.ListByStatusAsync(BillingExportJobStatus.Queued, cancellationToken).ConfigureAwait(false);
        if (queued.Any(j => j.PayerCode == payer && j.Period == period))
        {
            throw new DomainException(
                $"Eligibility violated: a queued export already exists for payer {payer.Value} and period {period.Start:O}..{period.End:O}.");
        }
    }
}
