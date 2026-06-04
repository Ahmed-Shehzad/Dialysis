using Dialysis.CQRS.Commands;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;
using Dialysis.PDMS.TreatmentSessions.Domain;

namespace Dialysis.PDMS.TreatmentSessions.Features.ScheduleSession;

public sealed record ScheduleSessionCommand : ICommand<Guid>, IPermissionedCommand
{
    public ScheduleSessionCommand(Guid PatientId,
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
    {
        this.PatientId = PatientId;
        this.ScheduledStartUtc = ScheduledStartUtc;
        this.DialyzerModel = DialyzerModel;
        this.PrescribedDurationMinutes = PrescribedDurationMinutes;
        this.BloodFlowRateMlPerMin = BloodFlowRateMlPerMin;
        this.DialysateFlowRateMlPerMin = DialysateFlowRateMlPerMin;
        this.DialysatePotassiumMmolPerL = DialysatePotassiumMmolPerL;
        this.DialysateCalciumMmolPerL = DialysateCalciumMmolPerL;
        this.DialysateSodiumMmolPerL = DialysateSodiumMmolPerL;
        this.TargetUfVolumeLiters = TargetUfVolumeLiters;
        this.AnticoagulationProtocolCode = AnticoagulationProtocolCode;
        this.AccessKind = AccessKind;
        this.AccessSite = AccessSite;
        this.AccessEstablishedOn = AccessEstablishedOn;
    }
    public string RequiredPermission => PdmsPermissions.SessionSchedule;
    public Guid PatientId { get; init; }
    public DateTime ScheduledStartUtc { get; init; }
    public string DialyzerModel { get; init; }
    public int PrescribedDurationMinutes { get; init; }
    public int BloodFlowRateMlPerMin { get; init; }
    public int DialysateFlowRateMlPerMin { get; init; }
    public decimal DialysatePotassiumMmolPerL { get; init; }
    public decimal DialysateCalciumMmolPerL { get; init; }
    public decimal DialysateSodiumMmolPerL { get; init; }
    public decimal TargetUfVolumeLiters { get; init; }
    public string AnticoagulationProtocolCode { get; init; }
    public VascularAccessKind AccessKind { get; init; }
    public string AccessSite { get; init; }
    public DateOnly AccessEstablishedOn { get; init; }
    public void Deconstruct(out Guid PatientId, out DateTime ScheduledStartUtc, out string DialyzerModel, out int PrescribedDurationMinutes, out int BloodFlowRateMlPerMin, out int DialysateFlowRateMlPerMin, out decimal DialysatePotassiumMmolPerL, out decimal DialysateCalciumMmolPerL, out decimal DialysateSodiumMmolPerL, out decimal TargetUfVolumeLiters, out string AnticoagulationProtocolCode, out VascularAccessKind AccessKind, out string AccessSite, out DateOnly AccessEstablishedOn)
    {
        PatientId = this.PatientId;
        ScheduledStartUtc = this.ScheduledStartUtc;
        DialyzerModel = this.DialyzerModel;
        PrescribedDurationMinutes = this.PrescribedDurationMinutes;
        BloodFlowRateMlPerMin = this.BloodFlowRateMlPerMin;
        DialysateFlowRateMlPerMin = this.DialysateFlowRateMlPerMin;
        DialysatePotassiumMmolPerL = this.DialysatePotassiumMmolPerL;
        DialysateCalciumMmolPerL = this.DialysateCalciumMmolPerL;
        DialysateSodiumMmolPerL = this.DialysateSodiumMmolPerL;
        TargetUfVolumeLiters = this.TargetUfVolumeLiters;
        AnticoagulationProtocolCode = this.AnticoagulationProtocolCode;
        AccessKind = this.AccessKind;
        AccessSite = this.AccessSite;
        AccessEstablishedOn = this.AccessEstablishedOn;
    }
}
