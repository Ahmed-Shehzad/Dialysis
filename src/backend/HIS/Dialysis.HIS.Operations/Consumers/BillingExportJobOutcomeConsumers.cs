using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIS.Operations.Domain.Enumerations;
using Dialysis.HIS.Operations.Ports;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIS.Operations.Consumers;

/// <summary>
/// Listens for <see cref="BillingExportJobCompletedIntegrationEvent"/> from EHR and flips the matching
/// HIS <c>BillingExportJob</c> from <c>Queued</c> to <c>Completed</c>. Idempotent: a job that is no
/// longer Queued (re-delivery, or a second EHR report) is left untouched.
/// </summary>
public sealed class BillingExportJobCompletedConsumer : IConsumer<BillingExportJobCompletedIntegrationEvent>
{
    private readonly IBillingExportJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BillingExportJobCompletedConsumer> _logger;
    /// <summary>
    /// Listens for <see cref="BillingExportJobCompletedIntegrationEvent"/> from EHR and marks the
    /// matching HIS export job completed.
    /// </summary>
    public BillingExportJobCompletedConsumer(IBillingExportJobRepository jobs,
        IUnitOfWork unitOfWork,
        ILogger<BillingExportJobCompletedConsumer> logger)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task HandleAsync(ConsumeContext<BillingExportJobCompletedIntegrationEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;
        var job = await _jobs.GetForUpdateAsync(message.JobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            _logger.LogWarning("Billing export job {JobId} completed by EHR but not found in HIS.", message.JobId);
            return;
        }
        if (job.Status != BillingExportJobStatus.Queued)
        {
            _logger.LogDebug("Billing export job {JobId} already {Status}; ignoring completion.", message.JobId, job.Status.Name);
            return;
        }

        job.MarkCompleted(message.CompletedAtUtc);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Billing export job {JobId} completed: {ClaimCount} claim(s), {BilledTotal} {Currency}.",
            message.JobId, message.ClaimCount, message.BilledTotal, message.CurrencyCode);
    }
}

/// <summary>
/// Listens for <see cref="BillingExportJobFailedIntegrationEvent"/> from EHR and flips the matching
/// HIS <c>BillingExportJob</c> from <c>Queued</c> to <c>Failed</c>. Idempotent on non-Queued jobs.
/// </summary>
public sealed class BillingExportJobFailedConsumer : IConsumer<BillingExportJobFailedIntegrationEvent>
{
    private readonly IBillingExportJobRepository _jobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BillingExportJobFailedConsumer> _logger;
    /// <summary>
    /// Listens for <see cref="BillingExportJobFailedIntegrationEvent"/> from EHR and marks the matching
    /// HIS export job failed.
    /// </summary>
    public BillingExportJobFailedConsumer(IBillingExportJobRepository jobs,
        IUnitOfWork unitOfWork,
        ILogger<BillingExportJobFailedConsumer> logger)
    {
        _jobs = jobs;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task HandleAsync(ConsumeContext<BillingExportJobFailedIntegrationEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;
        var job = await _jobs.GetForUpdateAsync(message.JobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            _logger.LogWarning("Billing export job {JobId} failed in EHR but not found in HIS.", message.JobId);
            return;
        }
        if (job.Status != BillingExportJobStatus.Queued)
        {
            _logger.LogDebug("Billing export job {JobId} already {Status}; ignoring failure.", message.JobId, job.Status.Name);
            return;
        }

        job.MarkFailed(message.FailedAtUtc);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogWarning("Billing export job {JobId} failed in EHR: {Reason}.", message.JobId, message.Reason);
    }
}
