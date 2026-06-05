namespace Dialysis.HIE.Inbound.Mpi;

/// <summary>Steward decision state for a candidate duplicate pair.</summary>
public enum PatientLinkReviewStatus
{
    /// <summary>Awaiting a steward's decision.</summary>
    Pending = 0,

    /// <summary>Steward confirmed the two records are the same person.</summary>
    Linked = 1,

    /// <summary>Steward confirmed the two records are distinct people.</summary>
    Rejected = 2,
}

/// <summary>
/// A probable-duplicate pair surfaced for human (steward) adjudication. Created when an inbound
/// patient reference scores a <see cref="MatchGrade.Probable"/> match against an existing index
/// entry from a different source — strong enough to suspect a duplicate, not strong enough to
/// auto-link. The steward links (same person) or rejects (distinct). Stored, never auto-resolved.
/// </summary>
public sealed class PatientLinkReview
{
    public Guid Id { get; private set; }

    public Guid SourceEntryId { get; private set; }
    public string SourcePartnerId { get; private set; } = string.Empty;
    public string SourceLabel { get; private set; } = string.Empty;

    public Guid CandidateEntryId { get; private set; }
    public string CandidatePartnerId { get; private set; } = string.Empty;
    public string CandidateLabel { get; private set; } = string.Empty;

    public double Score { get; private set; }
    public string Grade { get; private set; } = string.Empty;
    public PatientLinkReviewStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? ReviewNote { get; private set; }

    private PatientLinkReview() { }

    /// <summary>Raises a new pending review for a suspected duplicate pair.</summary>
    public static PatientLinkReview Raise(
        Guid sourceEntryId, string sourcePartnerId, string sourceLabel,
        Guid candidateEntryId, string candidatePartnerId, string candidateLabel,
        double score, MatchGrade grade, DateTime nowUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SourceEntryId = sourceEntryId,
            SourcePartnerId = sourcePartnerId,
            SourceLabel = sourceLabel,
            CandidateEntryId = candidateEntryId,
            CandidatePartnerId = candidatePartnerId,
            CandidateLabel = candidateLabel,
            Score = score,
            Grade = grade.ToString(),
            Status = PatientLinkReviewStatus.Pending,
            CreatedAtUtc = nowUtc,
        };

    /// <summary>Records the steward's adjudication (link = same person, else distinct).</summary>
    public void Resolve(bool linked, string reviewedBy, string? note, DateTime nowUtc)
    {
        if (Status != PatientLinkReviewStatus.Pending)
            throw new InvalidOperationException("This match review has already been resolved.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);

        Status = linked ? PatientLinkReviewStatus.Linked : PatientLinkReviewStatus.Rejected;
        ReviewedBy = reviewedBy.Trim();
        ReviewedAtUtc = nowUtc;
        ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
