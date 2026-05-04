using System.Text.Json;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Dialysis.BuildingBlocks.Transponder.Scheduling.Quartz;

/// <summary>Maps <see cref="ITransponderMessageScheduler"/> to Quartz triggers. Cron uses Quartz's cron syntax.</summary>
public sealed class QuartzTransponderMessageScheduler(
    ISchedulerFactory schedulerFactory,
    IMessageSerializer serializer,
    ILogger<QuartzTransponderMessageScheduler> logger) : ITransponderMessageScheduler
{
    private const string OnceGroup = "transponder-once";
    private const string RecurringGroup = "transponder-recurring";

    public async Task<string> ScheduleOnceAsync<TMessage>(
        TMessage message,
        DateTimeOffset runAt,
        TransponderPublishOptions? publishOptions = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var envelope = TransponderScheduledEnvelopeFactory.Create(message, serializer, publishOptions);
        var jobName = Guid.NewGuid().ToString("N");
        var jobKey = new JobKey(jobName, OnceGroup);
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);

        var job = JobBuilder.Create<TransponderQuartzPublishJob>()
            .WithIdentity(jobKey)
            .UsingJobData(TransponderQuartzPublishJob.EnvelopeJobDataKey, JsonSerializer.Serialize(envelope))
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{jobName}-trigger", OnceGroup)
            .StartAt(runAt)
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Scheduled one-time Transponder Quartz job {JobKey} for {RunAt}", jobKey, runAt);
        return jobName;
    }

    public async Task<string> ScheduleRecurringAsync<TMessage>(
        TMessage message,
        string cronExpression,
        TransponderPublishOptions? publishOptions = null,
        string? scheduleId = null,
        TimeZoneInfo? timeZone = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        var envelope = TransponderScheduledEnvelopeFactory.Create(message, serializer, publishOptions);
        var name = string.IsNullOrWhiteSpace(scheduleId) ? $"transponder:{typeof(TMessage).FullName}:{Guid.NewGuid():N}" : scheduleId!;
        var jobKey = new JobKey(name, RecurringGroup);
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);

        var job = JobBuilder.Create<TransponderQuartzPublishJob>()
            .WithIdentity(jobKey)
            .UsingJobData(TransponderQuartzPublishJob.EnvelopeJobDataKey, JsonSerializer.Serialize(envelope))
            .Build();

        var tz = timeZone ?? TimeZoneInfo.Utc;
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{name}-cron", RecurringGroup)
            .WithCronSchedule(cronExpression, b => b.InTimeZone(tz))
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Registered recurring Transponder Quartz job {JobKey} with cron {Cron}", jobKey, cronExpression);
        return name;
    }

    public async Task<bool> TryCancelOnceAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        return await scheduler.DeleteJob(new JobKey(scheduleId, OnceGroup), cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryCancelRecurringAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        return await scheduler.DeleteJob(new JobKey(scheduleId, RecurringGroup), cancellationToken).ConfigureAwait(false);
    }
}
