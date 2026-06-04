using Asp.Versioning;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Operator management surface for the per-payer / per-CPT fee schedule that backs
/// <c>EfCptFeeSchedule</c>. The charge consumer reads the most-specific matching row on every
/// administered service line; this controller is the write/query side an operator uses to seed
/// rates at onboarding and revise them on every payer rate change. Drives
/// <c>/admin/billing/fee-schedule</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing/fee-schedule")]
public sealed class FeeScheduleController : ControllerBase
{
    private readonly ICptFeeScheduleAdminRepository _entries;
    private readonly IUnitOfWork _unitOfWork;
    /// <summary>
    /// Operator management surface for the per-payer / per-CPT fee schedule that backs
    /// <c>EfCptFeeSchedule</c>. The charge consumer reads the most-specific matching row on every
    /// administered service line; this controller is the write/query side an operator uses to seed
    /// rates at onboarding and revise them on every payer rate change. Drives
    /// <c>/admin/billing/fee-schedule</c>.
    /// </summary>
    public FeeScheduleController(ICptFeeScheduleAdminRepository entries,
        IUnitOfWork unitOfWork)
    {
        _entries = entries;
        _unitOfWork = unitOfWork;
    }
    /// <summary>Lists fee-schedule rows, optionally narrowed by CPT and/or payer code.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FeeScheduleRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? cptCode = null,
        [FromQuery] string? payerCode = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await _entries.ListAsync(cptCode, payerCode, cancellationToken).ConfigureAwait(false);
        return Ok(rows.Select(FeeScheduleRow.From).ToArray());
    }

    /// <summary>Adds a new fee-schedule row. Use payer code <c>*</c> for the wildcard default rate.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(FeeScheduleRow), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] UpsertFeeScheduleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CptFeeScheduleEntry entry;
        try
        {
            entry = new CptFeeScheduleEntry(
                Guid.CreateVersion7(),
                request.CptCode,
                request.PayerCode,
                new Money(request.Amount, request.CurrencyCode),
                request.EffectiveFromUtc,
                request.EffectiveUntilUtc);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        _entries.Add(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(ListAsync), null, FeeScheduleRow.From(entry));
    }

    /// <summary>Revises an existing row's rate and effective window. CPT + payer are immutable.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FeeScheduleRow), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviseAsync(
        Guid id,
        [FromBody] UpsertFeeScheduleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await _entries.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (entry is null) return NotFound();

        try
        {
            entry.Revise(
                new Money(request.Amount, request.CurrencyCode),
                request.EffectiveFromUtc,
                request.EffectiveUntilUtc);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(FeeScheduleRow.From(entry));
    }

    /// <summary>Deletes a fee-schedule row.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _entries.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (entry is null) return NotFound();
        _entries.Remove(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}

public sealed record UpsertFeeScheduleRequest
{
    public UpsertFeeScheduleRequest(string CptCode,
        string PayerCode,
        decimal Amount,
        string CurrencyCode,
        DateOnly EffectiveFromUtc,
        DateOnly? EffectiveUntilUtc)
    {
        this.CptCode = CptCode;
        this.PayerCode = PayerCode;
        this.Amount = Amount;
        this.CurrencyCode = CurrencyCode;
        this.EffectiveFromUtc = EffectiveFromUtc;
        this.EffectiveUntilUtc = EffectiveUntilUtc;
    }
    public string CptCode { get; init; }
    public string PayerCode { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; }
    public DateOnly EffectiveFromUtc { get; init; }
    public DateOnly? EffectiveUntilUtc { get; init; }
    public void Deconstruct(out string CptCode, out string PayerCode, out decimal Amount, out string CurrencyCode, out DateOnly EffectiveFromUtc, out DateOnly? EffectiveUntilUtc)
    {
        CptCode = this.CptCode;
        PayerCode = this.PayerCode;
        Amount = this.Amount;
        CurrencyCode = this.CurrencyCode;
        EffectiveFromUtc = this.EffectiveFromUtc;
        EffectiveUntilUtc = this.EffectiveUntilUtc;
    }
}

public sealed record FeeScheduleRow
{
    public FeeScheduleRow(Guid Id,
        string CptCode,
        string PayerCode,
        decimal Amount,
        string CurrencyCode,
        DateOnly EffectiveFromUtc,
        DateOnly? EffectiveUntilUtc)
    {
        this.Id = Id;
        this.CptCode = CptCode;
        this.PayerCode = PayerCode;
        this.Amount = Amount;
        this.CurrencyCode = CurrencyCode;
        this.EffectiveFromUtc = EffectiveFromUtc;
        this.EffectiveUntilUtc = EffectiveUntilUtc;
    }
    public static FeeScheduleRow From(CptFeeScheduleEntry e) => new(
        Id: e.Id,
        CptCode: e.CptCode,
        PayerCode: e.PayerCode,
        Amount: e.Amount.Amount,
        CurrencyCode: e.Amount.CurrencyCode,
        EffectiveFromUtc: e.EffectiveFromUtc,
        EffectiveUntilUtc: e.EffectiveUntilUtc);
    public Guid Id { get; init; }
    public string CptCode { get; init; }
    public string PayerCode { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; }
    public DateOnly EffectiveFromUtc { get; init; }
    public DateOnly? EffectiveUntilUtc { get; init; }
    public void Deconstruct(out Guid Id, out string CptCode, out string PayerCode, out decimal Amount, out string CurrencyCode, out DateOnly EffectiveFromUtc, out DateOnly? EffectiveUntilUtc)
    {
        Id = this.Id;
        CptCode = this.CptCode;
        PayerCode = this.PayerCode;
        Amount = this.Amount;
        CurrencyCode = this.CurrencyCode;
        EffectiveFromUtc = this.EffectiveFromUtc;
        EffectiveUntilUtc = this.EffectiveUntilUtc;
    }
}
