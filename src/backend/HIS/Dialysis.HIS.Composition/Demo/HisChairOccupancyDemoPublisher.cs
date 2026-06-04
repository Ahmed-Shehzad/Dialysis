using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIS.Composition.Demo;

/// <summary>
/// Development-only one-shot broadcaster. The in-memory queue seeds two demo patients straight
/// into chairs but deliberately clears the raised events so the outbox isn't backfilled at
/// startup. This re-emits a <see cref="PatientPlacedInChairIntegrationEvent"/> for each seeded
/// in-chair entry (once, after consumers have bound their queues) so PDMS's chair-occupancy
/// projection — and the chair board — populate cross-module exactly as they would in production.
/// </summary>
public sealed class HisChairOccupancyDemoPublisher : BackgroundService
{
    // Give PDMS time to bind its consumer queue so the broadcast isn't published into the void.
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(20);

    private readonly IServiceProvider _services;
    private readonly ILogger<HisChairOccupancyDemoPublisher> _logger;

    /// <summary>Creates the publisher.</summary>
    public HisChairOccupancyDemoPublisher(
        IServiceProvider services,
        ILogger<HisChairOccupancyDemoPublisher> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            using var scope = _services.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<IPatientQueueRepository>();
            var bus = scope.ServiceProvider.GetRequiredService<ITransponderBus>();
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);

            var inChairs = queue.ListForToday(today)
                .Where(e => e.Status == QueueStatus.InTreatment && !string.IsNullOrWhiteSpace(e.Chair))
                .ToList();

            foreach (var entry in inChairs)
            {
                await bus.PublishAsync(
                    new PatientPlacedInChairIntegrationEvent(
                        EventId: Guid.CreateVersion7(),
                        OccurredOn: now,
                        SchemaVersion: 1,
                        EntryId: entry.Id,
                        PatientId: entry.PatientId,
                        Chair: entry.Chair!,
                        PlacedAtUtc: now),
                    stoppingToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "HIS chair-occupancy demo publisher: broadcast {Count} chair placements.", inChairs.Count);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HIS chair-occupancy demo publisher failed; chair board may be empty.");
        }
    }
}
