using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.EnqueueWaitlistEntry;

public sealed record EnqueueWaitlistEntryCommand : ICommand<Guid>, IPermissionedCommand
{
    public EnqueueWaitlistEntryCommand(Guid PatientId,
        string ResourceKindCode,
        string Notes,
        DateTime RequestedNotBeforeUtc)
    {
        this.PatientId = PatientId;
        this.ResourceKindCode = ResourceKindCode;
        this.Notes = Notes;
        this.RequestedNotBeforeUtc = RequestedNotBeforeUtc;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public Guid PatientId { get; init; }
    public string ResourceKindCode { get; init; }
    public string Notes { get; init; }
    public DateTime RequestedNotBeforeUtc { get; init; }
    public void Deconstruct(out Guid PatientId, out string ResourceKindCode, out string Notes, out DateTime RequestedNotBeforeUtc)
    {
        PatientId = this.PatientId;
        ResourceKindCode = this.ResourceKindCode;
        Notes = this.Notes;
        RequestedNotBeforeUtc = this.RequestedNotBeforeUtc;
    }
}
