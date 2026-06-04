using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.ClearPatientAlert;

public sealed record ClearPatientAlertCommand : ICommand, IPermissionedCommand
{
    public ClearPatientAlertCommand(Guid AlertId) => this.AlertId = AlertId;
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public Guid AlertId { get; init; }
    public void Deconstruct(out Guid alertId) => alertId = this.AlertId;
}
