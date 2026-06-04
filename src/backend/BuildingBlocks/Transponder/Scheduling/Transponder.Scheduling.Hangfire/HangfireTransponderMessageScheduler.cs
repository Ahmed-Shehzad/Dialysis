using Dialysis.BuildingBlocks.Transponder.Serialization;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Scheduling.Hangfire;

/// <summary>Maps <see cref="ITransponderMessageScheduler"/> to Hangfire delayed and recurring jobs.</summary>
public sealed class HangfireTransponderMessageScheduler : ITransponderMessageScheduler
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<HangfireTransponderMessageScheduler> _logger;
    /// <summary>Maps <see cref="ITransponderMessageScheduler"/> to Hangfire delayed and recurring jobs.</summary>
    public HangfireTransponderMessageScheduler(IBackgroundJobClient backgroundJobs,
        IMessageSerializer serializer,
        ILogger<HangfireTransponderMessageScheduler> logger)
    {
        _backgroundJobs = backgroundJobs;
        _serializer = serializer;
        _logger = logger;
    }
    public Task<string> ScheduleOnceAsync<TMessage>(
        TMessage message,
        DateTimeOffset runAt,
        TransponderPublishOptions? publishOptions = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var envelope = TransponderScheduledEnvelopeFactory.Create(message, _serializer, publishOptions);
        var id = _backgroundJobs.Schedule<TransponderHangfirePublishJob>(
            j => j.ExecuteAsync(envelope),
            runAt);
        _logger.LogDebug("Scheduled one-time Transponder publish {JobId} for {RunAt}", id, runAt);
        return Task.FromResult(id);
    }

    public Task<string> ScheduleRecurringAsync<TMessage>(
        TMessage message,
        string cronExpression,
        TransponderPublishOptions? publishOptions = null,
        string? scheduleId = null,
        TimeZoneInfo? timeZone = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        var envelope = TransponderScheduledEnvelopeFactory.Create(message, _serializer, publishOptions);
        var id = string.IsNullOrWhiteSpace(scheduleId) ? $"transponder:{typeof(TMessage).FullName}:{Guid.NewGuid():N}" : scheduleId!;
        RecurringJob.AddOrUpdate<TransponderHangfirePublishJob>(
            id,
            j => j.ExecuteAsync(envelope),
            cronExpression,
            new RecurringJobOptions { TimeZone = timeZone ?? TimeZoneInfo.Utc });
        _logger.LogDebug("Registered recurring Transponder publish {RecurringJobId} with cron {Cron}", id, cronExpression);
        return Task.FromResult(id);
    }

    public Task<bool> TryCancelOnceAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deleted = BackgroundJob.Delete(scheduleId);
        return Task.FromResult(deleted);
    }

    public Task<bool> TryCancelRecurringAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RecurringJob.RemoveIfExists(scheduleId);
        // Hangfire does not expose whether the id existed; treat as success once RemoveIfExists returns.
        return Task.FromResult(true);
    }
}
