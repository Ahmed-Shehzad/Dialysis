using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.MergePatients;

public sealed record MergePatientsCommand(Guid SurvivingPatientId, Guid SupersededPatientId)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PatientMerge;
}
