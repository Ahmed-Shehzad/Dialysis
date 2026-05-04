using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Integration.Features.IngestDeviceReading;

public sealed record IngestDeviceReadingCommand(string DeviceId, Guid PatientId, string PayloadJson, string? ExternalMessageId = null)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DeviceIngest;
}
