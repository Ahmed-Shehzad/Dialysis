using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.RecordProblem;

public sealed class RecordProblemCommandHandler(
    IProblemListRepository problems,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordProblemCommand, Guid>
{
    public async Task<Guid> Handle(RecordProblemCommand request, CancellationToken cancellationToken)
    {
        var condition = new Coding(request.ConditionSystem, request.ConditionCode, request.ConditionDisplay);
        var id = Guid.CreateVersion7();
        var item = ProblemListItem.Record(id, request.PatientId, condition, request.OnsetDate, request.Notes);
        problems.Add(item);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
