using Dialysis.HIE.Outbound.Domain;

namespace Dialysis.HIE.Outbound.Ports;

public interface IOutboundBundleStore
{
    Task AddAsync(OutboundBundle bundle, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboundBundle>> ClaimPendingAsync(int batchSize, DateTime asOfUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Operator dashboard read — every bundle ordered most-recent first. Optional status
    /// filter narrows the view; <c>null</c> returns rows in every state.
    /// </summary>
    Task<IReadOnlyList<OutboundBundle>> ListAsync(OutboundBundleStatus? statusFilter, int take, CancellationToken cancellationToken = default);

    /// <summary>Loads a single bundle by id for the retry command. Returns <c>null</c> on miss.</summary>
    Task<OutboundBundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every bundle for one patient, most-recent first. Backs the care-summary assembler, which
    /// aggregates the FHIR resources HIE has already mapped for the patient into a C-CDA CCD.
    /// </summary>
    Task<IReadOnlyList<OutboundBundle>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
