using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace Dialysis.BuildingBlocks.Transponder.Scheduling.TickerQ;

/// <summary>Maps <see cref="ITransponderMessageScheduler"/> to TickerQ time and cron tickers.</summary>
public sealed class TickerQTransponderMessageScheduler(
    ITimeTickerManager<TimeTickerEntity> timeTickers,
    ICronTickerManager<CronTickerEntity> cronTickers,
    IMessageSerializer serializer,
    ILogger<TickerQTransponderMessageScheduler> logger) : ITransponderMessageScheduler
{
    public async Task<string> ScheduleOnceAsync<TMessage>(
        TMessage message,
        DateTimeOffset runAt,
        TransponderPublishOptions? publishOptions = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var envelope = TransponderScheduledEnvelopeFactory.Create(message, serializer, publishOptions);
        var id = Guid.NewGuid();
        var entity = new TimeTickerEntity
        {
            Id = id,
            Function = TransponderTickerQPublishJobs.FunctionName,
            Description = $"Transponder publish {typeof(TMessage).Name}",
            ExecutionTime = runAt.UtcDateTime,
            Request = JsonSerializer.SerializeToUtf8Bytes(envelope),
        };

        await timeTickers.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("TickerQ time ticker {Id} scheduled for {RunAt}", id, runAt);
        return id.ToString("N");
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
        Guid id;
        if (string.IsNullOrWhiteSpace(scheduleId))
            id = Guid.NewGuid();
        else if (!Guid.TryParse(scheduleId, out id))
        {
            throw new ArgumentException(
                "TickerQ recurring scheduleId must be a Guid (N or D format) so the ticker row can be addressed for cancel/update.",
                nameof(scheduleId));
        }
        _ = timeZone; // TickerQ cron evaluation uses host/scheduler timezone; keep parameter for API parity with other schedulers.
        var entity = new CronTickerEntity
        {
            Id = id,
            Function = TransponderTickerQPublishJobs.FunctionName,
            Description = $"Transponder recurring {typeof(TMessage).Name}",
            Expression = cronExpression,
            Request = JsonSerializer.SerializeToUtf8Bytes(envelope),
            IsEnabled = true,
        };

        await cronTickers.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("TickerQ cron ticker {Id} registered with {Cron}", id, cronExpression);
        return id.ToString("N");
    }

    public async Task<bool> TryCancelOnceAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(scheduleId, out var id))
            return false;
        await timeTickers.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> TryCancelRecurringAsync(string scheduleId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(scheduleId, out var id))
            return false;
        await cronTickers.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
