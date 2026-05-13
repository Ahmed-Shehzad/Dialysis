using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.RecordMedicationStatement;

public sealed class RecordMedicationStatementCommandHandler(
    IMedicationStatementRepository medications,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordMedicationStatementCommand, Guid>
{
    public async Task<Guid> Handle(RecordMedicationStatementCommand request, CancellationToken cancellationToken)
    {
        var medication = new Coding(EhrCodeSystems.Rxnorm, request.MedicationRxnormCode, request.MedicationDisplay);
        var id = Guid.CreateVersion7();
        var statement = MedicationStatement.Record(
            id,
            request.PatientId,
            medication,
            request.DoseText,
            request.FrequencyText,
            request.StartedOn,
            request.ReasonText);
        medications.Add(statement);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
