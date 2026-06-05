using Dialysis.HIE.Inbound.Mpi;

namespace Dialysis.HIE.Inbound.Ports;

/// <summary>Persistence port for the MPI steward review queue (<see cref="PatientLinkReview"/>).</summary>
public interface IPatientLinkReviewStore
{
    void Add(PatientLinkReview review);

    Task<PatientLinkReview?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientLinkReview>> ListPendingAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>True when a review (in any state) already exists for this unordered entry pair — dedup guard.</summary>
    Task<bool> ExistsForPairAsync(Guid entryA, Guid entryB, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
