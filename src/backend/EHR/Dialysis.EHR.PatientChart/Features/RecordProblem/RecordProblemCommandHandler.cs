using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.RecordProblem;

public sealed class RecordProblemCommandHandler : ICommandHandler<RecordProblemCommand, Guid>
{
    private readonly IProblemListRepository _problems;
    private readonly IUnitOfWork _unitOfWork;
    public RecordProblemCommandHandler(IProblemListRepository problems,
        IUnitOfWork unitOfWork)
    {
        _problems = problems;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RecordProblemCommand request, CancellationToken cancellationToken)
    {
        var condition = new Coding(request.ConditionSystem, request.ConditionCode, request.ConditionDisplay);
        var id = Guid.CreateVersion7();
        var item = ProblemListItem.Record(id, request.PatientId, condition, request.OnsetDate, request.Notes);
        _problems.Add(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
