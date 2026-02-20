using BuildingBlocks.Abstractions;
using BuildingBlocks.Persistence;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Text.Json;

using Transponder.Abstractions;

namespace BuildingBlocks;

/// <summary>
/// Background service that reads pending integration events from the Outbox table,
/// publishes to Transponder and in-process handlers, then marks them processed.
/// Survives server restarts — events are persisted before publish.
/// </summary>
public sealed class IntegrationEventOutboxPublisher<TContext> : BackgroundService
    where TContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private const int BatchSize = 50;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrationEventOutboxPublisher<TContext>> _logger;

    public IntegrationEventOutboxPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<IntegrationEventOutboxPublisher<TContext>> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publisher error");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        List<IntegrationEventOutboxEntity> pending = await context.Set<IntegrationEventOutboxEntity>()
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        foreach (IntegrationEventOutboxEntity row in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                Type? eventType = Type.GetType(row.EventType);
                if (eventType is null)
                {
                    row.Error = $"Type not found: {row.EventType}";
                    row.ProcessedAtUtc = DateTimeOffset.UtcNow;
                    continue;
                }

                object? evt = JsonSerializer.Deserialize(row.Payload, eventType, JsonOptions);
                if (evt is not IIntegrationEvent integrationEvent)
                {
                    row.Error = "Deserialized object is not IIntegrationEvent";
                    row.ProcessedAtUtc = DateTimeOffset.UtcNow;
                    continue;
                }

                await publishEndpoint.PublishAsync(integrationEvent, cancellationToken);
                await publisher.PublishAsync(integrationEvent, cancellationToken);
                row.ProcessedAtUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                row.Error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                _logger.LogWarning(ex, "Failed to publish outbox row {Id}", row.Id);
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
