using Asp.Versioning;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Medications.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

/// <summary>
/// Pharmacy inventory dashboard backing the operator's <c>/admin/inventory</c> page.
/// Read-only listing, plus receive / deduct / adjust operator actions. Stock changes
/// are persisted through the aggregate so the low-stock integration event fires on
/// the same code path that the OnMedicationAdministered consumer uses.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/inventory")]
public sealed class InventoryController(
    IPdmsRepository<MedicationInventoryItem, Guid> inventory) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] bool lowStockOnly = false,
        CancellationToken cancellationToken = default)
    {
        var items = await inventory.ListAsync(null, cancellationToken).ConfigureAwait(false);
        IEnumerable<MedicationInventoryItem> filtered = items;
        if (lowStockOnly)
            filtered = items.Where(i => i.OnHandUnits <= i.Threshold);
        return Ok(filtered.Select(InventoryItemDto.From).ToArray());
    }

    [HttpPost("{id:guid}/receive")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReceiveAsync(
        Guid id,
        [FromBody] ReceiveStockRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await inventory.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return NotFound();
        item.Receive(request.Units, request.Reason);
        inventory.Update(item);
        return Ok(InventoryItemDto.From(item));
    }

    [HttpPost("{id:guid}/adjust")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdjustAsync(
        Guid id,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await inventory.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return NotFound();
        item.Adjust(request.NewOnHandUnits, request.Reason);
        inventory.Update(item);
        return Ok(InventoryItemDto.From(item));
    }
}

public sealed record ReceiveStockRequest(int Units, string Reason);

public sealed record AdjustStockRequest(int NewOnHandUnits, string Reason);

public sealed record InventoryItemDto(
    Guid Id,
    string MedicationCodeSystem,
    string MedicationCode,
    string MedicationDisplay,
    string LotNumber,
    DateTime ExpiryUtc,
    int OnHandUnits,
    int Threshold,
    bool LowStock)
{
    public static InventoryItemDto From(MedicationInventoryItem item) => new(
        Id: item.Id,
        MedicationCodeSystem: item.Medication.CodeSystem,
        MedicationCode: item.Medication.Code,
        MedicationDisplay: item.Medication.DisplayName,
        LotNumber: item.LotNumber,
        ExpiryUtc: item.ExpiryUtc,
        OnHandUnits: item.OnHandUnits,
        Threshold: item.Threshold,
        LowStock: item.OnHandUnits <= item.Threshold);
}
