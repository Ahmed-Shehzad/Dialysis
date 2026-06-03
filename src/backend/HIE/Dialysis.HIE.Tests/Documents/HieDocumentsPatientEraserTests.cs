using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Erasure;
using Dialysis.HIE.Documents.Ports;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents;

public sealed class HieDocumentsPatientEraserTests
{
    [Fact]
    public async Task Erase_Async_Returns_Zero_When_Patient_Has_Nothing_Async()
    {
        var sut = new HieDocumentsPatientEraser(
            new Stub_Repository([]),
            new InMemoryDocumentBlobStore(),
            new Stub_Unit_Of_Work(),
            NullLogger<HieDocumentsPatientEraser>.Instance);

        var result = await sut.EraseAsync(Guid.NewGuid(), "dpo", CancellationToken.None);

        result.RecordsErased.ShouldBe(0);
        result.ByCategory.ShouldBeEmpty();
    }

    [Fact]
    public async Task Erase_Async_Tombstones_And_Counts_By_Source_Async()
    {
        var patient = Guid.NewGuid();
        var docs = new[]
        {
            MakeDocument(patient, DocumentReferenceSource.PdmsReporting),
            MakeDocument(patient, DocumentReferenceSource.PdmsReporting),
            MakeDocument(patient, DocumentReferenceSource.HieInbound),
            MakeDocument(patient, DocumentReferenceSource.AdminUpload),
        };
        var blobs = new InMemoryDocumentBlobStore();
        foreach (var d in docs)
            await blobs.SaveAsync(d.Id, d.MimeType, new byte[] { 1, 2 }, CancellationToken.None);
        var uow = new Stub_Unit_Of_Work();

        var sut = new HieDocumentsPatientEraser(new Stub_Repository(docs), blobs, uow, NullLogger<HieDocumentsPatientEraser>.Instance);

        var result = await sut.EraseAsync(patient, "dpo", CancellationToken.None);

        result.RecordsErased.ShouldBe(4);
        result.ByCategory["PdmsReporting"].ShouldBe(2);
        result.ByCategory["HieInbound"].ShouldBe(1);
        result.ByCategory["AdminUpload"].ShouldBe(1);
        docs.ShouldAllBe(d => d.Status == DocumentReferenceStatus.EnteredInError);
        docs.ShouldAllBe(d => d.StorageRef == "purged://erasure");
        uow.Saves.ShouldBe(1);
    }

    private static DocumentReference MakeDocument(Guid patient, DocumentReferenceSource source) => new(
        id: Guid.CreateVersion7(),
        patientId: patient,
        kind: "DischargeLetter",
        title: "doc",
        mimeType: "application/pdf",
        storageRef: "inmem://documents/" + Guid.NewGuid().ToString("N"),
        contentHash: "AA",
        size: 1,
        source: source,
        createdAtUtc: DateTime.UtcNow);

    private sealed class Stub_Repository(IReadOnlyList<DocumentReference> docs) : IDocumentReferenceRepository
    {
        public void Add(DocumentReference document) { }
        public Task<DocumentReference?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<DocumentReference?>(null);
        public Task<IReadOnlyList<DocumentReference>> ListAsync(Guid? patientId, string? kind, DocumentReferenceStatus? status, DocumentReferenceSource? source, int take, CancellationToken cancellationToken) =>
            Task.FromResult(docs);
        public Task<IReadOnlyList<DocumentReference>> ListExpiredAsync(string kind, DateTime createdBefore, int take, CancellationToken cancellationToken) =>
            Task.FromResult(docs);
        public Task<IReadOnlyList<DocumentReference>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
            Task.FromResult(docs);
    }

    private sealed class Stub_Unit_Of_Work : IUnitOfWork
    {
        public int Saves { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.FromResult(0); }
    }
}
