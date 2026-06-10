using System.Collections.Concurrent;

namespace Dialysis.PDMS.TreatmentSessions.Projections;

/// <summary>
/// In-memory snapshot of which patient currently occupies which chair, kept current by
/// <see cref="Features.IngestChairAssignment.PatientPlacedInChairConsumer"/>. The chairside
/// view consumes this to resolve patient context from a chair id without round-tripping
/// HIS. A new placement on a previously-occupied chair overwrites the prior occupant —
/// the chair is the key, occupational reality forbids two patients on one chair.
/// </summary>
public sealed class ChairOccupancyProjection
{
    private readonly ConcurrentDictionary<string, ChairAssignmentSnapshot> _byChair = new(StringComparer.Ordinal);

    /// <summary>
    /// Records a patient placement on a chair. If the chair is already occupied, the prior
    /// occupant is implicitly released — the chair always reflects the latest placement.
    /// </summary>
    public void Place(Guid patientId, string chair, DateTime placedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chair);
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient required.", nameof(patientId));

        _byChair[chair.Trim()] = new ChairAssignmentSnapshot(chair.Trim(), patientId, placedAtUtc);
    }

    public IReadOnlyList<ChairAssignmentSnapshot> List() =>
        [.. _byChair.Values.OrderBy(a => a.Chair, StringComparer.Ordinal)];
}

public sealed record ChairAssignmentSnapshot
{
    public ChairAssignmentSnapshot(string Chair, Guid PatientId, DateTime PlacedAtUtc)
    {
        this.Chair = Chair;
        this.PatientId = PatientId;
        this.PlacedAtUtc = PlacedAtUtc;
    }
    public string Chair { get; init; }
    public Guid PatientId { get; init; }
    public DateTime PlacedAtUtc { get; init; }
    public void Deconstruct(out string Chair, out Guid PatientId, out DateTime PlacedAtUtc)
    {
        Chair = this.Chair;
        PatientId = this.PatientId;
        PlacedAtUtc = this.PlacedAtUtc;
    }
}
