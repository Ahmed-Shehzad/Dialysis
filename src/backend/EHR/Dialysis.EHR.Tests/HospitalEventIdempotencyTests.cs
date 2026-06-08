using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.ReadModels;
using Dialysis.EHR.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.EHR.Tests;

/// <summary>
/// Locks down idempotent projection writes under a race: two writers that both miss the fast-path
/// dedup read (the concurrent case) and insert the same conflict key with DIFFERENT primary ids must
/// NOT surface the unique-index violation. <see cref="IHospitalEventRepository.RecordAsync"/> and
/// <see cref="IAdverseEventRepository.RecordAsync"/> resolve the loser as an idempotent no-op.
/// Exercised against the real Postgres Testcontainer where the unique index is actually enforced (the
/// in-memory provider would not reproduce the conflict).
/// </summary>
[Collection(nameof(EhrFixtureCollection))]
public sealed class HospitalEventIdempotencyTests
{
    private readonly EhrApiWebApplicationFactory _factory;
    public HospitalEventIdempotencyTests(EhrApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Concurrent_Hospital_Event_Same_Key_Resolves_To_Single_Row_Async()
    {
        // Unique per run so the assertion is isolated from other tests sharing the container.
        var sourceEventKey = "race-" + Guid.NewGuid().ToString("N");

        // Two writers, SAME (Kind, SourceEventKey) but DIFFERENT primary ids, each in its own DI scope
        // (own DbContext) — exactly the shape a lost race produces. Neither must throw.
        await Record_Hospital_Event_Async(sourceEventKey);
        await Record_Hospital_Event_Async(sourceEventKey);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();
        var count = await db.HospitalEvents.AsNoTracking()
            .CountAsync(e => e.Kind == HospitalEventKind.ExternalEncounter && e.SourceEventKey == sourceEventKey, CancellationToken.None);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task Concurrent_Adverse_Event_Same_Key_Resolves_To_Single_Row_Async()
    {
        var sourceEventKey = "race-" + Guid.NewGuid().ToString("N");

        await Record_Adverse_Event_Async(sourceEventKey);
        await Record_Adverse_Event_Async(sourceEventKey);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();
        var count = await db.AdverseEvents.AsNoTracking()
            .CountAsync(e => e.SourceEventKey == sourceEventKey, CancellationToken.None);
        count.ShouldBe(1);
    }

    private async Task Record_Hospital_Event_Async(string sourceEventKey)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IHospitalEventRepository>();
        await repository.RecordAsync(
            new HospitalEvent
            {
                Id = Guid.CreateVersion7(),
                Kind = HospitalEventKind.ExternalEncounter,
                Source = "partner-race",
                OccurredAtUtc = DateTime.UtcNow,
                Detail = "race",
                ExternalPatientRef = "ext-race",
                SourceEventKey = sourceEventKey,
            },
            CancellationToken.None);
    }

    private async Task Record_Adverse_Event_Async(string sourceEventKey)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAdverseEventRepository>();
        await repository.RecordAsync(
            new AdverseEventRecord
            {
                Id = Guid.CreateVersion7(),
                PatientId = Guid.CreateVersion7(),
                SessionId = Guid.CreateVersion7(),
                Kind = "271594007",
                Severity = "moderate",
                Detail = "race",
                OccurredAtUtc = DateTime.UtcNow,
                SourceEventKey = sourceEventKey,
            },
            CancellationToken.None);
    }
}
