using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Hosted;
using Dialysis.HIE.Documents.Ports;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents;

public sealed class HieRetentionPurgeJobTests
{
    private static readonly DateTime _fixedNow = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Run_Once_With_No_Policies_Is_A_No_Op_Async()
    {
        var sut = MakeSut(policies: [], documents: []);
        var purged = await sut.Job.RunOnceAsync(CancellationToken.None);
        purged.ShouldBe(0);
        sut.UnitOfWork.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task Run_Once_Purges_Documents_Older_Than_Policy_Window_Async()
    {
        var policy = new DocumentRetentionPolicy(
            id: Guid.CreateVersion7(), kind: "DischargeLetter", retentionDays: 30,
            createdAtUtc: _fixedNow.AddDays(-90), updatedBy: "dpo");

        var fresh = MakeDocument("DischargeLetter", createdAt: _fixedNow.AddDays(-10));
        var expired = MakeDocument("DischargeLetter", createdAt: _fixedNow.AddDays(-45));
        var otherKind = MakeDocument("BillingDocument", createdAt: _fixedNow.AddDays(-200));

        var sut = MakeSut([policy], [fresh, expired, otherKind]);
        var purged = await sut.Job.RunOnceAsync(CancellationToken.None);

        purged.ShouldBe(1);
        expired.Status.ShouldBe(DocumentReferenceStatus.EnteredInError);
        expired.StorageRef.ShouldBe("purged://retention");
        fresh.Status.ShouldBe(DocumentReferenceStatus.Current);
        otherKind.Status.ShouldBe(DocumentReferenceStatus.Current);
        sut.UnitOfWork.Saves.ShouldBe(1);
    }

    private static DocumentReference MakeDocument(string kind, DateTime createdAt) => new(
        id: Guid.CreateVersion7(),
        patientId: Guid.NewGuid(),
        kind: kind,
        title: "doc",
        mimeType: "application/pdf",
        storageRef: "inmem://documents/" + Guid.NewGuid().ToString("N"),
        contentHash: "AA",
        size: 1,
        source: DocumentReferenceSource.PdmsReporting,
        createdAtUtc: createdAt);

    private static (HieRetentionPurgeJob Job, StubUnitOfWork UnitOfWork) MakeSut(
        IReadOnlyList<DocumentRetentionPolicy> policies,
        IReadOnlyList<DocumentReference> documents)
    {
        var uow = new StubUnitOfWork();
        var clock = new FixedClock(_fixedNow);
        var job = new HieRetentionPurgeJob(
            new StubPolicyRepository(policies),
            new StubDocumentRepository(documents),
            new InMemoryDocumentBlobStore(),
            uow,
            clock,
            NullLogger<HieRetentionPurgeJob>.Instance);
        return (job, uow);
    }

    private sealed class StubPolicyRepository(IReadOnlyList<DocumentRetentionPolicy> policies) : IDocumentRetentionPolicyRepository
    {
        public void Add(DocumentRetentionPolicy policy) { }
        public void Remove(DocumentRetentionPolicy policy) { }
        public Task<DocumentRetentionPolicy?> FindByKindAsync(string kind, CancellationToken cancellationToken) =>
            Task.FromResult(policies.FirstOrDefault(p => p.Kind == kind));
        public Task<IReadOnlyList<DocumentRetentionPolicy>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(policies);
    }

    private sealed class StubDocumentRepository(IReadOnlyList<DocumentReference> documents) : IDocumentReferenceRepository
    {
        public void Add(DocumentReference document) { }
        public Task<DocumentReference?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<DocumentReference?>(null);
        public Task<IReadOnlyList<DocumentReference>> ListAsync(Guid? patientId, string? kind, DocumentReferenceStatus? status, DocumentReferenceSource? source, int take, CancellationToken cancellationToken) =>
            Task.FromResult(documents);
        public Task<IReadOnlyList<DocumentReference>> ListExpiredAsync(string kind, DateTime createdBefore, int take, CancellationToken cancellationToken)
        {
            IReadOnlyList<DocumentReference> filtered = [.. documents
                .Where(d => d.Status == DocumentReferenceStatus.Current
                    && d.Kind == kind
                    && d.CreatedAtUtc < createdBefore)
                .Take(take)];
            return Task.FromResult(filtered);
        }
        public Task<IReadOnlyList<DocumentReference>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
            Task.FromResult(documents);
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int Saves { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.FromResult(0); }
    }

    private sealed class FixedClock(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }
}
