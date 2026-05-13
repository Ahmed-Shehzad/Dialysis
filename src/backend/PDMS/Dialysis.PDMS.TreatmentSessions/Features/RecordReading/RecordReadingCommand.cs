using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.RecordReading;

public sealed record RecordReadingCommand(
    Guid SessionId,
    int SystolicBloodPressure,
    int DiastolicBloodPressure,
    int HeartRateBpm,
    decimal ArterialPressureMmHg,
    decimal VenousPressureMmHg,
    decimal UltrafiltrationRateMlPerHour,
    decimal ConductivityMsPerCm,
    string? Notes)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.ReadingRecord;
}
