using Dialysis.HIS.Medication.Domain;

namespace Dialysis.HIS.Medication.Ports;

public interface IMedicationOrderRepository
{
    void Add(MedicationOrder order);

    Task<MedicationOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
