using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Mpi;
using Dialysis.HIE.Inbound.Mpi.Features;
using Dialysis.HIE.Inbound.Ports;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Mpi;

/// <summary>Coverage for the probabilistic match service + the steward review-queue domain/handler.</summary>
public sealed class MpiReviewQueueTests
{
    private static readonly DateOnly _dob = new(1980, 5, 1);

    private static PatientIndexEntry Entry(string partner, string ext, string? mrn, string family, string given) =>
        new(partner, ext, mrn, family, given, _dob, "male", DateTime.UtcNow);

    [Fact]
    public async Task Match_Service_Scores_And_Ranks_Candidates_Async()
    {
        var index = new FakeIndex(
            Entry("partner-a", "a1", "MRN-1", "Smith", "John"),   // strong match
            Entry("partner-b", "b1", null, "Smithe", "Jon"),       // fuzzy match
            Entry("partner-c", "c1", "MRN-9", "Johnson", "Alice")); // no match (still a DOB-block candidate)

        var service = new PatientMatchService(index, new PatientMatchScorer(new MpiMatchOptions()));
        var results = await service.FindMatchesAsync(
            new PatientMatchCriteria("MRN-1", "Smith", "John", _dob, "male"), take: 10, CancellationToken.None);

        results.Count.ShouldBeGreaterThanOrEqualTo(2);
        results[0].Entry.MedicalRecordNumber.ShouldBe("MRN-1"); // best score first
        results.ShouldAllBe(r => r.Grade != MatchGrade.NoMatch); // NoMatch dropped
        results.ShouldNotContain(r => r.Entry.FamilyName == "Johnson");
    }

    [Fact]
    public void Review_Raise_Is_Pending_Then_Resolves()
    {
        var review = PatientLinkReview.Raise(
            Guid.NewGuid(), "partner-a", "Smith, John 1980-05-01 [partner-a]",
            Guid.NewGuid(), "partner-b", "Smith, John 1980-05-01 [partner-b]",
            0.84, MatchGrade.Probable, DateTime.UtcNow);

        review.Status.ShouldBe(PatientLinkReviewStatus.Pending);
        review.Grade.ShouldBe("Probable");

        review.Resolve(linked: true, reviewedBy: "steward-1", note: "same patient", nowUtc: DateTime.UtcNow);
        review.Status.ShouldBe(PatientLinkReviewStatus.Linked);
        review.ReviewedBy.ShouldBe("steward-1");

        Should.Throw<InvalidOperationException>(() => review.Resolve(false, "steward-2", null, DateTime.UtcNow));
    }

    [Fact]
    public async Task Resolve_Handler_Records_Decision_Async()
    {
        var review = PatientLinkReview.Raise(
            Guid.NewGuid(), "partner-a", "a", Guid.NewGuid(), "partner-b", "b", 0.8, MatchGrade.Probable, DateTime.UtcNow);
        var store = new FakeReviewStore(review);
        var handler = new ResolvePatientLinkReviewCommandHandler(store, TimeProvider.System);

        await handler.HandleAsync(new ResolvePatientLinkReviewCommand(review.Id, Link: false, Note: "distinct", ReviewedBy: "steward-1"), CancellationToken.None);

        review.Status.ShouldBe(PatientLinkReviewStatus.Rejected);
        store.Saved.ShouldBeTrue();
    }

    private sealed class FakeIndex : IPatientIndex
    {
        private readonly List<PatientIndexEntry> _entries;
        public FakeIndex(params PatientIndexEntry[] seed) => _entries = [.. seed];

        public Task<PatientIndexEntry> UpsertAsync(PatientIndexEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.FromResult(entry);
        }

        public Task<IReadOnlyList<PatientIndexEntry>> MatchAsync(string? mrn, string? family, string? given, DateOnly? dob, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PatientIndexEntry>>([.. _entries.Take(take)]);

        // Blocking: return everything (the scorer filters); the seed all share a DOB anyway.
        public Task<IReadOnlyList<PatientIndexEntry>> MatchCandidatesAsync(string? mrn, string? family, DateOnly? dob, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PatientIndexEntry>>([.. _entries.Take(take)]);
    }

    private sealed class FakeReviewStore : IPatientLinkReviewStore
    {
        private readonly List<PatientLinkReview> _store;
        public bool Saved { get; private set; }
        public FakeReviewStore(params PatientLinkReview[] seed) => _store = [.. seed];

        public void Add(PatientLinkReview review) => _store.Add(review);
        public Task<PatientLinkReview?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(r => r.Id == id));
        public Task<IReadOnlyList<PatientLinkReview>> ListPendingAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PatientLinkReview>>([.. _store.Where(r => r.Status == PatientLinkReviewStatus.Pending).Take(take)]);
        public Task<bool> ExistsForPairAsync(Guid entryA, Guid entryB, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.Any(r =>
                (r.SourceEntryId == entryA && r.CandidateEntryId == entryB) ||
                (r.SourceEntryId == entryB && r.CandidateEntryId == entryA)));
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }
}
