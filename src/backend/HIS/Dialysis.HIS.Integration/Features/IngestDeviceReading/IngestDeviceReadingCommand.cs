using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Integration.Features.IngestDeviceReading;

/// <summary>
/// Opted into the durable command bus the same way as PDMS RecordReading — see
/// <c>docs/architecture/durable-writes.md</c>. When the host's
/// <c>His:DurableCommands:IngestDeviceReading:Enabled</c> flag is true, the controller
/// publishes to the durable transport and returns 202; the consumer applies the write
/// through the existing handler. The flag stays off by default so the synchronous path
/// keeps working during rollout.
///
/// <see cref="ReadingId"/> is the durability-pattern's "id-from-command-id" trick:
/// callers can supply a stable id (defaults to <see cref="Guid.Empty"/>) and the handler
/// uses it as the new device reading's id, so a redelivery yields the same row and a 202
/// caller knows the id immediately without polling.
/// </summary>
[DurableCommand("his")]
public sealed record IngestDeviceReadingCommand(
    string DeviceId,
    Guid PatientId,
    string PayloadJson,
    string? ExternalMessageId = null,
    Guid ReadingId = default)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DeviceIngest;
}
