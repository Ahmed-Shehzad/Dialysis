using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.RecordVitalSign;

public sealed class RecordVitalSignCommandHandler(
    IVitalSignRepository vitals,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordVitalSignCommand, Guid>
{
    public async Task<Guid> Handle(RecordVitalSignCommand request, CancellationToken cancellationToken)
    {
        var observation = new Coding(EhrCodeSystems.Loinc, request.LoincCode, request.Display);
        var id = Guid.CreateVersion7();
        var reading = VitalSignReading.Record(
            id,
            request.PatientId,
            observation,
            request.Value,
            request.UnitCode,
            request.ObservedAtUtc,
            request.EncounterId,
            request.RecordedByProviderId);
        vitals.Add(reading);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
