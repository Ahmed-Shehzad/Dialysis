namespace Dialysis.EHR.Integration.ReadModels;

/// <summary>
/// A cross-patient projection of patient-safety adverse events (today: PDMS intradialytic events). Feeds
/// the safety-surveillance dashboard so clinical leadership can spot patterns — a spike in a given event
/// kind/severity over a window — that aren't visible one patient or one session at a time.
/// </summary>
public sealed class AdverseEventRecord
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public Guid SessionId { get; set; }

    /// <summary>The event kind code (SNOMED) — e.g. hypotension, cramping.</summary>
    public string Kind { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    /// <summary>Free-text notes from the source event.</summary>
    public string? Detail { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    /// <summary>The source integration event's id — the idempotency key (redelivery-safe).</summary>
    public string SourceEventKey { get; set; } = string.Empty;
}
