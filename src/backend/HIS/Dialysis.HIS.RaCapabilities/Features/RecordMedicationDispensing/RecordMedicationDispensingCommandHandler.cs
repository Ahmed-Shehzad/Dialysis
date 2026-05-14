using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RecordMedicationDispensing;

public sealed class RecordMedicationDispensingCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordMedicationDispensingCommand, Guid>
{
    public async Task<Guid> HandleAsync(RecordMedicationDispensingCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        store.AddMedicationDispensingRecord(
            new RaMedicationDispensingRecord
            {
                Id = id,
                MedicationOrderId = request.MedicationOrderId,
                BarcodeToken = request.BarcodeToken.Trim(),
                DispensedAtUtc = DateTime.UtcNow,
            });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
