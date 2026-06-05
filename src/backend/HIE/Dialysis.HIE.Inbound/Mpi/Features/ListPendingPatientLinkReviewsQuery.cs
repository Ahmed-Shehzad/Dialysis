using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Inbound.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Inbound.Mpi.Features;

/// <summary>The MPI steward's work queue — suspected-duplicate pairs awaiting adjudication.</summary>
public sealed record ListPendingPatientLinkReviewsQuery(int Take = 100)
    : IQuery<IReadOnlyList<PatientLinkReviewDto>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.MpiStewardReview;
}

/// <summary>A pending duplicate pair as shown in the steward queue.</summary>
public sealed record PatientLinkReviewDto(
    Guid Id,
    Guid SourceEntryId,
    string SourcePartnerId,
    string SourceLabel,
    Guid CandidateEntryId,
    string CandidatePartnerId,
    string CandidateLabel,
    double Score,
    string Grade,
    DateTime CreatedAtUtc);

public sealed class ListPendingPatientLinkReviewsQueryHandler
    : IQueryHandler<ListPendingPatientLinkReviewsQuery, IReadOnlyList<PatientLinkReviewDto>>
{
    private readonly IPatientLinkReviewStore _store;
    public ListPendingPatientLinkReviewsQueryHandler(IPatientLinkReviewStore store) => _store = store;

    public async Task<IReadOnlyList<PatientLinkReviewDto>> HandleAsync(
        ListPendingPatientLinkReviewsQuery request, CancellationToken cancellationToken)
    {
        var take = request.Take is > 0 and <= 500 ? request.Take : 100;
        var reviews = await _store.ListPendingAsync(take, cancellationToken).ConfigureAwait(false);
        return [.. reviews.Select(r => new PatientLinkReviewDto(
            r.Id, r.SourceEntryId, r.SourcePartnerId, r.SourceLabel,
            r.CandidateEntryId, r.CandidatePartnerId, r.CandidateLabel,
            r.Score, r.Grade, r.CreatedAtUtc))];
    }
}
