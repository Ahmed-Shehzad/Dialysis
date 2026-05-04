namespace Dialysis.HIS.Operations.Domain;

public sealed class InventoryItem
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public int QuantityOnHand { get; set; }
}
