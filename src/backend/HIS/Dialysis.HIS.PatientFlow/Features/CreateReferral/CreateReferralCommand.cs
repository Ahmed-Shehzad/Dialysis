using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.CreateReferral;

public sealed record CreateReferralCommand(Guid PatientId, string ReferralTypeCode)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.ReferralCreate;
}
