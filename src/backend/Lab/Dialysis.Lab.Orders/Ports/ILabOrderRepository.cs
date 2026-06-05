using Dialysis.Lab.Orders.Domain;

namespace Dialysis.Lab.Orders.Ports;

/// <summary>Persistence port for the <see cref="LabOrder"/> aggregate.</summary>
public interface ILabOrderRepository
{
    void Add(LabOrder order);

    Task<LabOrder?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Find by placer order number — the key an inbound result is matched on.</summary>
    Task<LabOrder?> FindByPlacerOrderNumberAsync(string placerOrderNumber, CancellationToken cancellationToken);

    Task<IReadOnlyList<LabOrder>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken);
}
