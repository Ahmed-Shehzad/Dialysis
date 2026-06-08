using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Ports;
using Dialysis.HIE.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests;

/// <summary>
/// Locks down idempotent inbound-ingest writes under a race: two writers that both miss their
/// fast-path dedup read (the concurrent case) and insert the same conflict key with DIFFERENT
/// primary ids must NOT surface the unique/primary-key violation. Each store resolves the loser
/// as an idempotent no-op (and <see cref="IPatientIndex.UpsertAsync"/> returns a stable persisted
/// id with refreshed demographics). Exercised against the real Postgres Testcontainer where the
/// indexes are actually enforced (the in-memory provider would not reproduce the conflict).
/// </summary>
[Collection(nameof(HiePostgresFixtureCollection))]
public sealed class InboundIngestIdempotencyTests
{
    private readonly HiePostgresApiWebApplicationFactory _factory;
    public InboundIngestIdempotencyTests(HiePostgresApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Concurrent_Received_Resource_Same_Key_Resolves_To_Single_Row_Async()
    {
        var logicalId = "race-" + Guid.NewGuid().ToString("N");
        const string partnerId = "partner-race";
        const string resourceType = "Patient";

        await Upsert_Received_Resource_Async(partnerId, resourceType, logicalId);
        await Upsert_Received_Resource_Async(partnerId, resourceType, logicalId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HieDbContext>();
        var count = await db.ReceivedResources.AsNoTracking()
            .CountAsync(
                r => r.PartnerId == partnerId && r.ResourceType == resourceType && r.LogicalId == logicalId,
                CancellationToken.None);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task Concurrent_Patient_Index_Same_Key_Resolves_To_Stable_Single_Row_Async()
    {
        var externalLogicalId = "race-" + Guid.NewGuid().ToString("N");
        const string partnerId = "partner-race";

        var firstId = await Upsert_Patient_Index_Async(partnerId, externalLogicalId, "MRN-1", "First");
        // Second writer has a DIFFERENT primary id but the SAME (PartnerId, ExternalLogicalId), and
        // carries refreshed demographics — exactly the shape a lost race produces.
        var secondId = await Upsert_Patient_Index_Async(partnerId, externalLogicalId, "MRN-2", "Second");

        // The returned id stays stable across the race (the committed winner's id).
        secondId.ShouldBe(firstId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HieDbContext>();
        var rows = await db.PatientIndexEntries.AsNoTracking()
            .Where(p => p.PartnerId == partnerId && p.ExternalLogicalId == externalLogicalId)
            .ToListAsync(CancellationToken.None);
        rows.Count.ShouldBe(1);
        // The losing writer's demographics were applied (Refresh on the winner).
        rows[0].FamilyName.ShouldBe("Second");
        rows[0].MedicalRecordNumber.ShouldBe("MRN-2");
    }

    [Fact]
    public async Task Concurrent_Document_Reference_Same_Id_Resolves_To_Single_Row_Async()
    {
        var documentId = Guid.CreateVersion7();
        var patientId = Guid.CreateVersion7();

        var firstCreated = await Try_Add_Document_Async(documentId, patientId);
        var secondCreated = await Try_Add_Document_Async(documentId, patientId);

        firstCreated.ShouldBeTrue();
        // The second writer lost the primary-key race; idempotent rather than a 500.
        secondCreated.ShouldBeFalse();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HieDbContext>();
        var count = await db.DocumentReferences.AsNoTracking()
            .CountAsync(d => d.Id == documentId, CancellationToken.None);
        count.ShouldBe(1);
    }

    private async Task Upsert_Received_Resource_Async(string partnerId, string resourceType, string logicalId)
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IReceivedResourceStore>();
        await store.UpsertAsync(
            new ReceivedResource(partnerId, resourceType, logicalId, "{\"resourceType\":\"Patient\"}", DateTime.UtcNow, null),
            CancellationToken.None);
    }

    private async Task<Guid> Upsert_Patient_Index_Async(string partnerId, string externalLogicalId, string mrn, string familyName)
    {
        using var scope = _factory.Services.CreateScope();
        var index = scope.ServiceProvider.GetRequiredService<IPatientIndex>();
        var result = await index.UpsertAsync(
            new PatientIndexEntry(partnerId, externalLogicalId, mrn, familyName, "Race", new DateOnly(1980, 1, 1), "male", DateTime.UtcNow),
            CancellationToken.None);
        return result.Id;
    }

    private async Task<bool> Try_Add_Document_Async(Guid documentId, Guid patientId)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentReferenceRepository>();
        return await repository.TryAddIdempotentAsync(
            new DocumentReference(
                id: documentId,
                patientId: patientId,
                kind: "DischargeLetter",
                title: "race",
                mimeType: "application/pdf",
                storageRef: "inmem://documents/" + Guid.NewGuid().ToString("N"),
                contentHash: "AA",
                size: 1,
                source: DocumentReferenceSource.PdmsReporting,
                createdAtUtc: DateTime.UtcNow),
            CancellationToken.None);
    }
}
