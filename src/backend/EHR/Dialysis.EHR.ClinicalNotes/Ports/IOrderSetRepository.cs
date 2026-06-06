using Dialysis.EHR.ClinicalNotes.Domain;

namespace Dialysis.EHR.ClinicalNotes.Ports;

public interface IOrderSetRepository
{
    Task<OrderSet?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists active order sets (lines included) for the picker.</summary>
    Task<IReadOnlyList<OrderSet>> ListActiveAsync(int take, CancellationToken cancellationToken = default);

    void Add(OrderSet orderSet);
}
