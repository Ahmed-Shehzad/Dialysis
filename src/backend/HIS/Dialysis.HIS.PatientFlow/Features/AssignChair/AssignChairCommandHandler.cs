using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.AssignChair;

public sealed class AssignChairCommandHandler : ICommandHandler<AssignChairCommand, Guid>
{
    private readonly IPatientQueueRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public AssignChairCommandHandler(IPatientQueueRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(AssignChairCommand request, CancellationToken cancellationToken)
    {
        var entry = _repository.Get(request.EntryId)
            ?? throw new DomainException("Queue entry not found.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (_repository.IsChairOccupied(today, request.Chair))
            throw new DomainException($"{request.Chair} is already in use.");
        entry.AssignChair(request.Chair, DateTime.UtcNow);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Id;
    }
}
