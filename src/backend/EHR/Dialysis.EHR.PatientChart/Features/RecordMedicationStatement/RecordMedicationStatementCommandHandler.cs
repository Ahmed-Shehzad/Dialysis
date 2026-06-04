using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.RecordMedicationStatement;

public sealed class RecordMedicationStatementCommandHandler : ICommandHandler<RecordMedicationStatementCommand, Guid>
{
    private readonly IMedicationStatementRepository _medications;
    private readonly IUnitOfWork _unitOfWork;
    public RecordMedicationStatementCommandHandler(IMedicationStatementRepository medications,
        IUnitOfWork unitOfWork)
    {
        _medications = medications;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RecordMedicationStatementCommand request, CancellationToken cancellationToken)
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
        _medications.Add(statement);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
