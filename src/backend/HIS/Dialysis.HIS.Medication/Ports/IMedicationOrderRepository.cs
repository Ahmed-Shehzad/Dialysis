using Dialysis.HIS.Medication.Domain;

namespace Dialysis.HIS.Medication.Ports;

public interface IMedicationOrderRepository
{
    Task<MedicationOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(MedicationOrder order);
}
