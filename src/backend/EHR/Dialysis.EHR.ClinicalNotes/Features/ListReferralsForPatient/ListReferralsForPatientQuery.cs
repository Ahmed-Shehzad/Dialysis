using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.ListReferralsForPatient;

/// <summary>Lists a patient's referrals (most-recent first) for the chart's referral history.</summary>
public sealed record ListReferralsForPatientQuery(Guid PatientId, int Take = 20)
    : IQuery<IReadOnlyList<ReferralDto>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.ClinicalNoteRead;
}

/// <summary>A referral row surfaced to the chart.</summary>
public sealed record ReferralDto(
    Guid Id,
    Guid PatientId,
    string DestinationPartnerId,
    Guid ReferringProviderId,
    string? ReferralReason,
    DateTime RequestedAtUtc);
