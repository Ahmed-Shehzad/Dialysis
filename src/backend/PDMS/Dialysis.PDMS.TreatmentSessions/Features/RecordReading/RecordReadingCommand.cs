using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.RecordReading;

/// <summary>
/// Opted in to the durable command bus via <see cref="DurableCommandAttribute"/>: when the
/// PDMS host has the bus configured, the controller can route the write through the durable
/// path (202 + status endpoint) instead of synchronous dispatch. The handler is unchanged —
/// the durable consumer dispatches into the same <c>ICommandHandler</c> through the existing
/// CQRS gateway. <see cref="ReadingId"/> lets a retrying client supply a stable id so a
/// redelivery produces the same reading row; defaults to <see cref="Guid.Empty"/> for
/// in-process callers, in which case the handler generates a fresh id.
/// </summary>
[DurableCommand("pdms")]
public sealed record RecordReadingCommand(
    Guid SessionId,
    int SystolicBloodPressure,
    int DiastolicBloodPressure,
    int HeartRateBpm,
    decimal ArterialPressureMmHg,
    decimal VenousPressureMmHg,
    decimal UltrafiltrationRateMlPerHour,
    decimal ConductivityMsPerCm,
    string? Notes,
    Guid ReadingId = default)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.ReadingRecord;
}
