using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordVitalSign;

/// <summary>
/// Vital-sign recording. Opted into the durable command bus (see
/// <c>docs/architecture/durable-writes.md</c>) — when
/// <c>Ehr:DurableCommands:RecordVitalSign:Enabled</c> is true the controller
/// returns 202 + a status URL instead of the synchronous 201. <see cref="ReadingId"/>
/// is the id-from-CommandId trick so a redelivery yields the same row and the 202
/// caller knows the id without polling.
/// </summary>
[DurableCommand("ehr")]
public sealed record RecordVitalSignCommand(
    Guid PatientId,
    Guid? EncounterId,
    string LoincCode,
    string? Display,
    decimal Value,
    string UnitCode,
    DateTime ObservedAtUtc,
    Guid? RecordedByProviderId,
    Guid ReadingId = default)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.VitalsRecord;
}
