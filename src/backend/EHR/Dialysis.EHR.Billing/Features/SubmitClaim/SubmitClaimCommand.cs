using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Billing.Features.SubmitClaim;

public sealed record SubmitClaimCommand(
    Guid PatientId,
    Guid PayerId,
    IReadOnlyList<Guid> ChargeIds,
    string ClaimFormatCode = EhrClaimFormats.Edi837Professional)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ClaimSubmit;
}
