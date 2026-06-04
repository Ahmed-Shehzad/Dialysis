using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RecordMedicationDispensing;

public sealed class RecordMedicationDispensingCommandHandler : ICommandHandler<RecordMedicationDispensingCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public RecordMedicationDispensingCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RecordMedicationDispensingCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        _store.AddMedicationDispensingRecord(
            new RaMedicationDispensingRecord
            {
                Id = id,
                MedicationOrderId = request.MedicationOrderId,
                BarcodeToken = request.BarcodeToken.Trim(),
                DispensedAtUtc = DateTime.UtcNow,
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
