using Dialysis.HIS.PatientFlow.Domain;

namespace Dialysis.HIS.PatientFlow.Ports;

/// <summary>
/// Read/write port for today's patient queue. Today this is backed by an in-memory store
/// seeded with demo entries; once the HIS Scheduling + PatientFlow slices commit to a
/// query shape an EF-backed implementation can replace it without changes to handlers.
/// </summary>
public interface IPatientQueueRepository
{
    IReadOnlyList<PatientQueueEntry> ListForToday(DateOnly today);
    PatientQueueEntry? Get(Guid id);
    void Add(PatientQueueEntry entry);
    /// <summary>Returns true when the chair is occupied by an in-treatment entry today.</summary>
    bool IsChairOccupied(DateOnly today, string chair);
}
