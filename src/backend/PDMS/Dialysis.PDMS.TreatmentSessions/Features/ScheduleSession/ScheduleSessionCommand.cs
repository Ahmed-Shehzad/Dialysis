using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;
using Dialysis.PDMS.TreatmentSessions.Domain;

namespace Dialysis.PDMS.TreatmentSessions.Features.ScheduleSession;

public sealed record ScheduleSessionCommand(
    Guid PatientId,
    DateTime ScheduledStartUtc,
    string DialyzerModel,
    int PrescribedDurationMinutes,
    int BloodFlowRateMlPerMin,
    int DialysateFlowRateMlPerMin,
    decimal DialysatePotassiumMmolPerL,
    decimal DialysateCalciumMmolPerL,
    decimal DialysateSodiumMmolPerL,
    decimal TargetUfVolumeLiters,
    string AnticoagulationProtocolCode,
    VascularAccessKind AccessKind,
    string AccessSite,
    DateOnly AccessEstablishedOn)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.SessionSchedule;
}
