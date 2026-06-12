using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.CheckInPatient;

public sealed class CheckInPatientCommandHandler : ICommandHandler<CheckInPatientCommand, Guid>
{
    private readonly IPatientQueueRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CheckInPatientCommandHandler(IPatientQueueRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(CheckInPatientCommand request, CancellationToken cancellationToken)
    {
        var entry = _repository.Get(request.EntryId)
            ?? throw new InvalidOperationException("Queue entry not found.");
        entry.CheckIn(request.ArrivalTimeUtc, request.EligibilityAcknowledged);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Id;
    }
}
