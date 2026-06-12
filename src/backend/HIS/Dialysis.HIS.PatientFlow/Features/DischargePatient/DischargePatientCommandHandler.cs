using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.DischargePatient;

public sealed class DischargePatientCommandHandler : ICommandHandler<DischargePatientCommand, Unit>
{
    private readonly IAdmissionRepository _admissions;
    private readonly IUnitOfWork _unitOfWork;
    public DischargePatientCommandHandler(IAdmissionRepository admissions,
        IUnitOfWork unitOfWork)
    {
        _admissions = admissions;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(DischargePatientCommand request, CancellationToken cancellationToken)
    {
        var admission = await _admissions.GetAsync(request.AdmissionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Admission '{request.AdmissionId}' not found.");
        admission.Discharge(DateTime.UtcNow);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
