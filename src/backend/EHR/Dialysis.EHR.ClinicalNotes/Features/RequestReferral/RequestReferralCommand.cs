using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.RequestReferral;

/// <summary>
/// Refers / transfers a patient to an external organisation. Persists a <c>Referral</c> and raises
/// <c>ReferralRequestedIntegrationEvent</c>, which HIE Outbound turns into a CCD push.
/// </summary>
public sealed record RequestReferralCommand(
    Guid PatientId,
    string DestinationPartnerId,
    Guid ReferringProviderId,
    string? ReferralReason) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ReferralRequest;
}
