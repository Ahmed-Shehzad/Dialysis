using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.ListReferralsForPatient;

public sealed class ListReferralsForPatientQueryHandler : IQueryHandler<ListReferralsForPatientQuery, IReadOnlyList<ReferralDto>>
{
    private readonly IReferralRepository _referrals;
    public ListReferralsForPatientQueryHandler(IReferralRepository referrals) => _referrals = referrals;

    public async Task<IReadOnlyList<ReferralDto>> HandleAsync(ListReferralsForPatientQuery request, CancellationToken cancellationToken)
    {
        var rows = await _referrals.ListByPatientAsync(request.PatientId, request.Take, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new ReferralDto(
            r.Id, r.PatientId, r.DestinationPartnerId, r.ReferringProviderId, r.ReferralReason, r.RequestedAtUtc))];
    }
}
