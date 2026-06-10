using Dialysis.BuildingBlocks.Hipaa.Encryption;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Dialysis.HIS.Tests;

/// <summary>
/// Horizontal-scaling guarantee for the outbox relay: when a module host runs multiple
/// replicas, only the replica holding the per-database advisory lock relays — the others
/// skip their tick. Exercised against real Postgres (the relay's lock path is
/// Npgsql-provider-gated, so an in-memory provider would bypass it).
/// </summary>
[Collection(nameof(HisFixtureCollection))]
public sealed class OutboxRelayAdvisoryLockTests
{
    private readonly HisApiWebApplicationFactory _factory;

    /// <summary>
    /// Horizontal-scaling guarantee for the outbox relay; see the class docs.
    /// </summary>
    public OutboxRelayAdvisoryLockTests(HisApiWebApplicationFactory factory) => _factory = factory;

    /// <summary>Payload type for the seeded outbox row — resolvable via its assembly-qualified name.</summary>
    public sealed record RelayLockProbeEvent(Guid ProbeId);

    [Fact]
    public async Task Relay_Skips_Tick_While_Another_Replica_Holds_The_Lock_And_Drains_After_Release_Async()
    {
        var probeId = Guid.NewGuid();
        Guid rowId;
        string connectionString;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
            connectionString = scope.ServiceProvider.GetRequiredService<IConfiguration>()
                .GetConnectionString("His")!;
            var row = new TransponderOutboxMessageEntity
            {
                Id = Guid.CreateVersion7(),
                AssemblyQualifiedEventType = typeof(RelayLockProbeEvent).AssemblyQualifiedName!,
                PayloadJson = $"{{\"probeId\":\"{probeId}\"}}",
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.OutboxMessages.Add(row);
            await db.SaveChangesAsync(CancellationToken.None);
            rowId = row.Id;
        }

        // A "second replica": hold the relay's advisory lock on an independent session.
        await using var rival = new NpgsqlConnection(connectionString);
        await rival.OpenAsync(CancellationToken.None);
        await using (var take = new NpgsqlCommand(
            $"SELECT pg_advisory_lock({TransponderOutboxRelayHostedService<HisDbContext>.AdvisoryLockKey})", rival))
        {
            await take.ExecuteNonQueryAsync(CancellationToken.None);
        }

        // The relay gets its own minimal DI graph: same database, but a no-op bus — the
        // host's in-memory transport rejects routing keys without a registered consumer,
        // and this test is about the lock, not about consumption.
        var services = new ServiceCollection();
        services.AddSingleton(_factory.Services.GetRequiredService<IOptions<TransponderPersistenceOptions>>());
        services.AddSingleton(_factory.Services.GetRequiredService<IPhiProtector>());
        services.AddDbContext<HisDbContext>(o => o.UseNpgsql(connectionString));
        services.AddSingleton<ITransponderBus, NoopTransponderBus>();
        services.AddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
        await using var provider = services.BuildServiceProvider();

        var relay = new TransponderOutboxRelayHostedService<HisDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TransponderOutboxRelayOptions
            {
                BatchSize = 10,
                IdlePollInterval = TimeSpan.FromMilliseconds(50),
            }),
            NullLogger<TransponderOutboxRelayHostedService<HisDbContext>>.Instance);

        await relay.StartAsync(CancellationToken.None);
        try
        {
            // Locked: several poll intervals pass and the row must stay pending.
            await Task.Delay(500);
            Assert.Null(await ReadProcessedAtAsync(rowId));

            // Lock released → the relay's next tick wins the lock and drains the row.
            await using (var release = new NpgsqlCommand(
                $"SELECT pg_advisory_unlock({TransponderOutboxRelayHostedService<HisDbContext>.AdvisoryLockKey})", rival))
            {
                await release.ExecuteNonQueryAsync(CancellationToken.None);
            }

            var deadline = DateTime.UtcNow.AddSeconds(10);
            DateTime? processedAt = null;
            while (processedAt is null && DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);
                processedAt = await ReadProcessedAtAsync(rowId);
            }
            Assert.NotNull(processedAt);
        }
        finally
        {
            await relay.StopAsync(CancellationToken.None);
        }
    }

    private async Task<DateTime?> ReadProcessedAtAsync(Guid rowId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        return await db.OutboxMessages.AsNoTracking()
            .Where(o => o.Id == rowId)
            .Select(o => o.ProcessedAtUtc)
            .SingleAsync(CancellationToken.None);
    }

    private sealed class NoopTransponderBus : ITransponderBus
    {
        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;
    }
}
