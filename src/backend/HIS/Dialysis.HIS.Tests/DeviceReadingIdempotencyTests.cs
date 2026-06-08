using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

/// <summary>
/// Locks down idempotent re-ingest under a race: two writes with the same ExternalMessageId that
/// both miss the handler's fast-path dedup read (the concurrent case) must NOT surface the unique
/// index violation. <see cref="IDeviceReadingRepository.PersistIdempotentAsync"/> resolves the
/// loser to the winning row's id. Exercised against the real Postgres Testcontainer where the
/// unique index is actually enforced (the in-memory provider would not reproduce the conflict).
/// </summary>
[Collection(nameof(HisFixtureCollection))]
public sealed class DeviceReadingIdempotencyTests
{
    private readonly HisApiWebApplicationFactory _factory;
    public DeviceReadingIdempotencyTests(HisApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Persist_Idempotent_Returns_Existing_Id_On_External_Message_Id_Conflict_Async()
    {
        // Unique per run so the assertion is isolated from other tests sharing the container.
        var externalMessageId = "race-" + Guid.NewGuid().ToString("N");

        // First writer commits the row.
        var first = await Persist_Async(externalMessageId);

        // Second writer has a DIFFERENT row id but the SAME ExternalMessageId, and goes straight to
        // PersistIdempotentAsync (skipping the handler's fast-path dedup read) — exactly the shape a
        // lost race produces. The unique index rejects it; the repository must resolve to the winner.
        var second = await Persist_Async(externalMessageId);

        second.ShouldBe(first);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var count = await db.DeviceReadings.AsNoTracking()
            .CountAsync(d => d.ExternalMessageId == externalMessageId, CancellationToken.None);
        count.ShouldBe(1);
    }

    private async Task<Guid> Persist_Async(string externalMessageId)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeviceReadingRepository>();
        return await repository.PersistIdempotentAsync(
            new DeviceReadingRecord
            {
                Id = Guid.CreateVersion7(),
                DeviceId = "race-device",
                PatientId = Guid.CreateVersion7(),
                PayloadJson = "{\"v\":1}",
                ReceivedAtUtc = DateTime.UtcNow,
                ExternalMessageId = externalMessageId,
            },
            CancellationToken.None);
    }
}
