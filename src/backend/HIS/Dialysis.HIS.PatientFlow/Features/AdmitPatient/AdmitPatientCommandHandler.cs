using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Domain.ValueObjects;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.AdmitPatient;

public sealed class AdmitPatientCommandHandler : ICommandHandler<AdmitPatientCommand, Guid>
{
    private readonly IAdmissionRepository _admissions;
    private readonly IUnitOfWork _unitOfWork;
    public AdmitPatientCommandHandler(IAdmissionRepository admissions,
        IUnitOfWork unitOfWork)
    {
        _admissions = admissions;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(AdmitPatientCommand request, CancellationToken cancellationToken)
    {
        var admission = Admission.Admit(request.PatientId, new WardCode(request.WardCode), DateTime.UtcNow);
        _admissions.Add(admission);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return admission.Id;
    }
}
