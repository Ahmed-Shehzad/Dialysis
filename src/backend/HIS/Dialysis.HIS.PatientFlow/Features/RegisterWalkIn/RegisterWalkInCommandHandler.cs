using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.RegisterWalkIn;

public sealed class RegisterWalkInCommandHandler : ICommandHandler<RegisterWalkInCommand, PatientQueueEntryDto>
{
    private readonly IPatientQueueRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterWalkInCommandHandler(IPatientQueueRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task<PatientQueueEntryDto> HandleAsync(
        RegisterWalkInCommand request,
        CancellationToken cancellationToken)
    {
        var entry = PatientQueueEntry.WalkIn(
            id: Guid.CreateVersion7(),
            patientId: Guid.CreateVersion7(),
            patientName: request.PatientName.Trim(),
            mrn: request.Mrn.Trim(),
            arrivalUtc: DateTime.UtcNow,
            eligibilityVerified: request.EligibilityVerified);
        _repository.Add(entry);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PatientQueueEntryDto(
            entry.Id,
            entry.PatientId,
            entry.PatientName,
            entry.Mrn,
            entry.ScheduledForUtc,
            "waiting",
            entry.Chair,
            entry.EligibilityVerified);
    }
}
