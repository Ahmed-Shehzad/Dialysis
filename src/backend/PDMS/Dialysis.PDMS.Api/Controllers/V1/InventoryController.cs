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
public sealed class InventoryController : ControllerBase
{
    private readonly IPdmsRepository<MedicationInventoryItem, Guid> _inventory;
    /// <summary>
    /// Pharmacy inventory dashboard backing the operator's <c>/admin/inventory</c> page.
    /// Read-only listing, plus receive / deduct / adjust operator actions. Stock changes
    /// are persisted through the aggregate so the low-stock integration event fires on
    /// the same code path that the OnMedicationAdministered consumer uses.
    /// </summary>
    public InventoryController(IPdmsRepository<MedicationInventoryItem, Guid> inventory) => _inventory = inventory;
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] bool lowStockOnly = false,
        CancellationToken cancellationToken = default)
    {
        var items = await _inventory.ListAsync(null, cancellationToken).ConfigureAwait(false);
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
        var item = await _inventory.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return NotFound();
        item.Receive(request.Units, request.Reason);
        _inventory.Update(item);
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
        var item = await _inventory.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return NotFound();
        item.Adjust(request.NewOnHandUnits, request.Reason);
        _inventory.Update(item);
        return Ok(InventoryItemDto.From(item));
    }

    /// <summary>
    /// Registers a new inventory row (one per <c>(medication, lot)</c>). Stock then flows through
    /// <c>receive</c> / <c>adjust</c> and is deducted by the OnMedicationAdministered consumer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        MedicationInventoryItem item;
        try
        {
            item = new MedicationInventoryItem(
                Guid.CreateVersion7(),
                new MedicationCoding(request.MedicationCodeSystem, request.MedicationCode, request.MedicationDisplay),
                request.LotNumber,
                request.ExpiryUtc,
                request.InitialOnHandUnits,
                request.Threshold);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        await _inventory.AddAsync(item, cancellationToken).ConfigureAwait(false);
        // Literal Location URI (not CreatedAtAction): URL-segment API versioning can't resolve the
        // {version} route value for action-link generation, which throws -> 500.
        return Created($"/api/v1.0/inventory/{item.Id}", InventoryItemDto.From(item));
    }
}

public sealed record CreateInventoryItemRequest
{
    public CreateInventoryItemRequest(string MedicationCodeSystem,
        string MedicationCode,
        string MedicationDisplay,
        string LotNumber,
        DateTime ExpiryUtc,
        int InitialOnHandUnits,
        int Threshold)
    {
        this.MedicationCodeSystem = MedicationCodeSystem;
        this.MedicationCode = MedicationCode;
        this.MedicationDisplay = MedicationDisplay;
        this.LotNumber = LotNumber;
        this.ExpiryUtc = ExpiryUtc;
        this.InitialOnHandUnits = InitialOnHandUnits;
        this.Threshold = Threshold;
    }
    public string MedicationCodeSystem { get; init; }
    public string MedicationCode { get; init; }
    public string MedicationDisplay { get; init; }
    public string LotNumber { get; init; }
    public DateTime ExpiryUtc { get; init; }
    public int InitialOnHandUnits { get; init; }
    public int Threshold { get; init; }
    public void Deconstruct(out string medicationCodeSystem, out string medicationCode, out string medicationDisplay, out string lotNumber, out DateTime expiryUtc, out int initialOnHandUnits, out int threshold)
    {
        medicationCodeSystem = this.MedicationCodeSystem;
        medicationCode = this.MedicationCode;
        medicationDisplay = this.MedicationDisplay;
        lotNumber = this.LotNumber;
        expiryUtc = this.ExpiryUtc;
        initialOnHandUnits = this.InitialOnHandUnits;
        threshold = this.Threshold;
    }
}

public sealed record ReceiveStockRequest
{
    public ReceiveStockRequest(int Units, string Reason)
    {
        this.Units = Units;
        this.Reason = Reason;
    }
    public int Units { get; init; }
    public string Reason { get; init; }
    public void Deconstruct(out int units, out string reason)
    {
        units = this.Units;
        reason = this.Reason;
    }
}

public sealed record AdjustStockRequest
{
    public AdjustStockRequest(int NewOnHandUnits, string Reason)
    {
        this.NewOnHandUnits = NewOnHandUnits;
        this.Reason = Reason;
    }
    public int NewOnHandUnits { get; init; }
    public string Reason { get; init; }
    public void Deconstruct(out int newOnHandUnits, out string reason)
    {
        newOnHandUnits = this.NewOnHandUnits;
        reason = this.Reason;
    }
}

public sealed record InventoryItemDto
{
    public InventoryItemDto(Guid Id,
        string MedicationCodeSystem,
        string MedicationCode,
        string MedicationDisplay,
        string LotNumber,
        DateTime ExpiryUtc,
        int OnHandUnits,
        int Threshold,
        bool LowStock)
    {
        this.Id = Id;
        this.MedicationCodeSystem = MedicationCodeSystem;
        this.MedicationCode = MedicationCode;
        this.MedicationDisplay = MedicationDisplay;
        this.LotNumber = LotNumber;
        this.ExpiryUtc = ExpiryUtc;
        this.OnHandUnits = OnHandUnits;
        this.Threshold = Threshold;
        this.LowStock = LowStock;
    }
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
    public Guid Id { get; init; }
    public string MedicationCodeSystem { get; init; }
    public string MedicationCode { get; init; }
    public string MedicationDisplay { get; init; }
    public string LotNumber { get; init; }
    public DateTime ExpiryUtc { get; init; }
    public int OnHandUnits { get; init; }
    public int Threshold { get; init; }
    public bool LowStock { get; init; }
    public void Deconstruct(out Guid id, out string medicationCodeSystem, out string medicationCode, out string medicationDisplay, out string lotNumber, out DateTime expiryUtc, out int onHandUnits, out int threshold, out bool lowStock)
    {
        id = this.Id;
        medicationCodeSystem = this.MedicationCodeSystem;
        medicationCode = this.MedicationCode;
        medicationDisplay = this.MedicationDisplay;
        lotNumber = this.LotNumber;
        expiryUtc = this.ExpiryUtc;
        onHandUnits = this.OnHandUnits;
        threshold = this.Threshold;
        lowStock = this.LowStock;
    }
}
