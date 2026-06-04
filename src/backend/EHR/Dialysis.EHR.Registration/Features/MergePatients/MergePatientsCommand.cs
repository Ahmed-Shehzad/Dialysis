using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.MergePatients;

public sealed record MergePatientsCommand : ICommand, IPermissionedCommand
{
    public MergePatientsCommand(Guid SurvivingPatientId, Guid SupersededPatientId)
    {
        this.SurvivingPatientId = SurvivingPatientId;
        this.SupersededPatientId = SupersededPatientId;
    }
    public string RequiredPermission => EhrPermissions.PatientMerge;
    public Guid SurvivingPatientId { get; init; }
    public Guid SupersededPatientId { get; init; }
    public void Deconstruct(out Guid SurvivingPatientId, out Guid SupersededPatientId)
    {
        SurvivingPatientId = this.SurvivingPatientId;
        SupersededPatientId = this.SupersededPatientId;
    }
}
