using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Operations.Domain.ValueObjects;

namespace Dialysis.HIS.Operations.Domain;

/// <summary>
/// Aggregate root: a tracked inventory item identified by <see cref="Sku"/> with a running quantity-on-hand
/// invariant of <c>QuantityOnHand &gt;= 0</c>.
/// </summary>
public sealed class InventoryItem : AggregateRoot<Guid>
{
    public Sku Sku { get; private set; } = null!;
    public int QuantityOnHand { get; private set; }

    private InventoryItem()
    {
    }

    private InventoryItem(Guid id) : base(id)
    {
    }

    public static InventoryItem Stock(Sku sku)
    {
        ArgumentNullException.ThrowIfNull(sku);
        return new InventoryItem(Guid.CreateVersion7())
        {
            Sku = sku,
            QuantityOnHand = 0,
        };
    }

    public void RecordMovement(int deltaQuantity)
    {
        var next = QuantityOnHand + deltaQuantity;
        if (next < 0)
            throw new DomainException($"InventoryItem quantity cannot go negative (current {QuantityOnHand}, delta {deltaQuantity}).");
        QuantityOnHand = next;
    }
}
