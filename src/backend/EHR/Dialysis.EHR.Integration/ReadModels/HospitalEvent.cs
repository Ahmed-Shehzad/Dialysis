namespace Dialysis.EHR.Integration.ReadModels;

/// <summary>What kind of hospital/encounter event this is.</summary>
public enum HospitalEventKind
{
    /// <summary>Patient admitted to a ward (internal HIS).</summary>
    Admitted = 1,

    /// <summary>Patient discharged from an admission (internal HIS).</summary>
    Discharged = 2,

    /// <summary>Patient seen/encountered at an outside organisation (HIE inbound).</summary>
    ExternalEncounter = 3,
}

/// <summary>
/// A care-coordination signal that a patient was in (or seen by) a hospital — fed by HIS admit/discharge
/// events and HIE external-encounter ingestion. Drives the "needs follow-up" worklist so the care team
/// can proactively follow up after a hospital stay.
///
/// <para>External encounters carry an opaque partner patient id, not our local patient Guid (EHR has no
/// MPI cross-reference), so <see cref="PatientId"/> is null and <see cref="ExternalPatientRef"/> is set
/// for manual linking.</para>
/// </summary>
public sealed class HospitalEvent
{
    public Guid Id { get; set; }

    /// <summary>Local patient id; null for an unmatched external encounter.</summary>
    public Guid? PatientId { get; set; }

    public HospitalEventKind Kind { get; set; }

    /// <summary>"HIS" for internal admit/discharge, or the HIE partner id for an external encounter.</summary>
    public string Source { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Ward (internal) or class/reason (external) — human-readable context.</summary>
    public string? Detail { get; set; }

    /// <summary>Partner patient logical id for an unmatched external encounter; else null.</summary>
    public string? ExternalPatientRef { get; set; }

    /// <summary>Dedup key (admission id / external logical id) so re-delivery doesn't duplicate.</summary>
    public string SourceEventKey { get; set; } = string.Empty;

    public bool FollowedUp { get; set; }

    public DateTime? FollowedUpAtUtc { get; set; }
}
