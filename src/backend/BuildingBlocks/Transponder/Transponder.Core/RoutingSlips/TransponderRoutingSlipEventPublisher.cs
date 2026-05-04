namespace Dialysis.BuildingBlocks.Transponder;

internal sealed class TransponderRoutingSlipEventPublisher(ITransponderBus bus)
{
    private static TransponderPublishOptions Opt(string? correlationId, string deduplicationId) =>
        new(correlationId, deduplicationId);

    public Task PublishActivityCompletedAsync(
        string trackingNumber,
        int activityIndex,
        string activityName,
        string? argumentsJson,
        string? correlationId,
        CancellationToken cancellationToken) =>
        bus.PublishAsync(
            new TransponderRoutingSlipActivityCompleted
            {
                TrackingNumber = trackingNumber,
                TimestampUtc = DateTimeOffset.UtcNow,
                ActivityIndex = activityIndex,
                ActivityName = activityName,
                ArgumentsJson = argumentsJson,
            },
            Opt(correlationId, $"{trackingNumber}:evt:activity-completed:{activityIndex}"),
            cancellationToken);

    public Task PublishActivityFaultedAsync(
        string trackingNumber,
        string activityName,
        int activityIndex,
        string? argumentsJson,
        string faultReason,
        string? faultExceptionDetail,
        string? correlationId,
        CancellationToken cancellationToken) =>
        bus.PublishAsync(
            new TransponderRoutingSlipActivityFaulted
            {
                TrackingNumber = trackingNumber,
                TimestampUtc = DateTimeOffset.UtcNow,
                ActivityName = activityName,
                ActivityIndex = activityIndex,
                ArgumentsJson = argumentsJson,
                FaultReason = faultReason,
                FaultExceptionDetail = faultExceptionDetail,
            },
            Opt(correlationId, $"{trackingNumber}:evt:activity-faulted:{activityIndex}"),
            cancellationToken);

    public Task PublishActivityCompensatedAsync(
        string trackingNumber,
        string activityName,
        int activityIndex,
        string? correlationId,
        CancellationToken cancellationToken) =>
        bus.PublishAsync(
            new TransponderRoutingSlipActivityCompensated
            {
                TrackingNumber = trackingNumber,
                TimestampUtc = DateTimeOffset.UtcNow,
                ActivityName = activityName,
                ActivityIndex = activityIndex,
            },
            Opt(correlationId, $"{trackingNumber}:evt:activity-compensated:{activityIndex}"),
            cancellationToken);

    public Task PublishActivityCompensationFailedAsync(
        string trackingNumber,
        string activityName,
        int activityIndex,
        string reason,
        string? exceptionDetail,
        string? correlationId,
        CancellationToken cancellationToken) =>
        bus.PublishAsync(
            new TransponderRoutingSlipActivityCompensationFailed
            {
                TrackingNumber = trackingNumber,
                TimestampUtc = DateTimeOffset.UtcNow,
                ActivityName = activityName,
                ActivityIndex = activityIndex,
                Reason = reason,
                ExceptionDetail = exceptionDetail,
            },
            Opt(correlationId, $"{trackingNumber}:evt:activity-compensation-failed:{activityIndex}"),
            cancellationToken);

    public Task PublishSlipCompensationFailedAsync(
        string trackingNumber,
        string reason,
        string? lastFailedActivityName,
        int? lastFailedActivityIndex,
        string? correlationId,
        CancellationToken cancellationToken) =>
        bus.PublishAsync(
            new TransponderRoutingSlipCompensationFailed
            {
                TrackingNumber = trackingNumber,
                TimestampUtc = DateTimeOffset.UtcNow,
                Reason = reason,
                LastFailedActivityName = lastFailedActivityName,
                LastFailedActivityIndex = lastFailedActivityIndex,
            },
            Opt(correlationId, $"{trackingNumber}:evt:slip-compensation-failed"),
            cancellationToken);

    public Task PublishSlipCompletedAsync(string trackingNumber, string? correlationId, CancellationToken cancellationToken) =>
        bus.PublishAsync(
            new TransponderRoutingSlipCompleted
            {
                TrackingNumber = trackingNumber,
                TimestampUtc = DateTimeOffset.UtcNow,
            },
            Opt(correlationId, $"{trackingNumber}:evt:slip-completed"),
            cancellationToken);

    public Task PublishSlipFaultedAsync(
        string trackingNumber,
        string faultReason,
        string? faultExceptionDetail,
        string? failedActivityName,
        int? failedActivityIndex,
        string? correlationId,
        CancellationToken cancellationToken) =>
        bus.PublishAsync(
            new TransponderRoutingSlipFaulted
            {
                TrackingNumber = trackingNumber,
                TimestampUtc = DateTimeOffset.UtcNow,
                FaultReason = faultReason,
                FaultExceptionDetail = faultExceptionDetail,
                FailedActivityName = failedActivityName,
                FailedActivityIndex = failedActivityIndex,
            },
            Opt(correlationId, $"{trackingNumber}:evt:slip-faulted"),
            cancellationToken);
}
