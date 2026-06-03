namespace Dialysis.HIE.Documents.Domain;

/// <summary>
/// Operator-mutable retention window for a single <see cref="DocumentReference.Kind"/>.
/// One row per kind (e.g. <c>"DischargeLetter"</c>, <c>"BillingDocument"</c>,
/// <c>"AdminUpload"</c>); the scheduled <c>RetentionPurgerHostedService</c> reads every
/// row and soft-deletes documents older than the configured window.
///
/// Distinct from the platform-wide <c>IRetentionSchedule</c> (BuildingBlocks): that schedule
/// captures the *regulatory floor* (BDSG §10, HGB §257 — usually a 10- or 30-year minimum
/// the operator cannot reduce). This aggregate is the *operator override* sitting above the
/// floor: the DPO sets per-kind windows in the admin UI matching the clinic's published
/// privacy policy. The purger enforces whichever is the longer of (floor, override).
/// </summary>
public sealed class DocumentRetentionPolicy
{
    public Guid Id { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public int RetentionDays { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string UpdatedBy { get; private set; } = string.Empty;

    private DocumentRetentionPolicy() { }

    public DocumentRetentionPolicy(
        Guid id,
        string kind,
        int retentionDays,
        DateTime createdAtUtc,
        string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
        if (retentionDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(retentionDays),
                "Retention window must be positive — a non-positive value would purge documents the same day they are produced.");

        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
        Kind = kind;
        RetentionDays = retentionDays;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        UpdatedBy = updatedBy;
    }

    /// <summary>Operator updated the window. Stamps the audit fields with the new actor.</summary>
    public void Revise(int retentionDays, DateTime now, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
        if (retentionDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(retentionDays));
        RetentionDays = retentionDays;
        UpdatedAtUtc = now;
        UpdatedBy = updatedBy;
    }
}
