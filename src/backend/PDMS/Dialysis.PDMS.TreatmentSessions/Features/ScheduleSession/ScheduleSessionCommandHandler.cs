using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.ScheduleSession;

public sealed class ScheduleSessionCommandHandler : ICommandHandler<ScheduleSessionCommand, Guid>
{
    private readonly IDialysisSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    public ScheduleSessionCommandHandler(IDialysisSessionRepository sessions,
        IUnitOfWork unitOfWork)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(ScheduleSessionCommand request, CancellationToken cancellationToken)
    {
        var prescription = new SessionPrescription(
            request.DialyzerModel,
            request.PrescribedDurationMinutes,
            request.BloodFlowRateMlPerMin,
            request.DialysateFlowRateMlPerMin,
            request.DialysatePotassiumMmolPerL,
            request.DialysateCalciumMmolPerL,
            request.DialysateSodiumMmolPerL,
            request.TargetUfVolumeLiters,
            request.AnticoagulationProtocolCode);

        var access = new VascularAccess(request.AccessKind, request.AccessSite, request.AccessEstablishedOn);

        var id = Guid.CreateVersion7();
        var session = DialysisSession.Schedule(id, request.PatientId, request.ScheduledStartUtc, prescription, access);
        _sessions.Add(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
