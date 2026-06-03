namespace Dialysis.EHR.Billing.Domain;

/// <summary>
/// One row in the per-payer / per-CPT / effective-date fee schedule. EHR.Billing's
/// charge consumer asks for the billed amount on every administered service line; the
/// EF-backed schedule resolves the most-specific matching row.
///
/// Resolution order (most specific wins):
/// <list type="number">
///   <item>Exact <c>PayerCode</c> + CPT, effective at or before the service date.</item>
///   <item>Wildcard payer (<c>"*"</c>) + CPT, effective at or before the service date.</item>
///   <item>The configurable / config-backed schedule's default. (PR 6 wired the fallback.)</item>
/// </list>
/// </summary>
public sealed class CptFeeScheduleEntry
{
    private CptFeeScheduleEntry() { }

    public CptFeeScheduleEntry(
        Guid id,
        string cptCode,
        string payerCode,
        Money amount,
        DateOnly effectiveFromUtc,
        DateOnly? effectiveUntilUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cptCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(payerCode);
        ArgumentNullException.ThrowIfNull(amount);
        if (effectiveUntilUtc.HasValue && effectiveUntilUtc.Value < effectiveFromUtc)
            throw new ArgumentException("EffectiveUntil must be on or after EffectiveFrom.", nameof(effectiveUntilUtc));
        Id = id;
        CptCode = cptCode.Trim().ToUpperInvariant();
        PayerCode = payerCode.Trim().ToUpperInvariant();
        Amount = amount;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveUntilUtc = effectiveUntilUtc;
    }

    public Guid Id { get; private set; }
    public string CptCode { get; private set; } = string.Empty;
    public string PayerCode { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public DateOnly EffectiveFromUtc { get; private set; }
    public DateOnly? EffectiveUntilUtc { get; private set; }

    public bool CoversInstant(DateOnly atUtc) =>
        atUtc >= EffectiveFromUtc && (!EffectiveUntilUtc.HasValue || atUtc <= EffectiveUntilUtc.Value);

    /// <summary>
    /// Revises this row's rate and effective window in place. Operators use this to correct a
    /// typo or extend an existing rate; superseding a rate with a new one (keeping the old for
    /// audit) is done by adding a fresh row instead. CPT and payer are immutable — change those
    /// by deleting and re-adding so the identity of "which schedule line" stays stable.
    /// </summary>
    public void Revise(Money amount, DateOnly effectiveFromUtc, DateOnly? effectiveUntilUtc)
    {
        ArgumentNullException.ThrowIfNull(amount);
        if (effectiveUntilUtc.HasValue && effectiveUntilUtc.Value < effectiveFromUtc)
            throw new ArgumentException("EffectiveUntil must be on or after EffectiveFrom.", nameof(effectiveUntilUtc));
        Amount = amount;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveUntilUtc = effectiveUntilUtc;
    }
}

/// <summary>
/// Per-session per-CPT idempotency marker. The charge consumer writes one row per
/// captured charge so re-delivery of the same <c>DialysisSessionChargeReadyIntegrationEvent</c>
/// short-circuits without double-billing.
/// </summary>
public sealed class ChargeIdempotencyMarker
{
    private ChargeIdempotencyMarker() { }

    public ChargeIdempotencyMarker(Guid sessionId, string cptCode, Guid chargeId, DateTime capturedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cptCode);
        SessionId = sessionId;
        CptCode = cptCode;
        ChargeId = chargeId;
        CapturedAtUtc = capturedAtUtc;
    }

    public Guid SessionId { get; private set; }
    public string CptCode { get; private set; } = string.Empty;
    public Guid ChargeId { get; private set; }
    public DateTime CapturedAtUtc { get; private set; }
}
