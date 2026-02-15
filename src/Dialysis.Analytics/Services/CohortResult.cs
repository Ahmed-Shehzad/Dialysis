namespace Dialysis.Analytics.Services;

public sealed record CohortResult
{
    public required IReadOnlyList<string> PatientIds { get; init; }
    public required IReadOnlyList<string> EncounterIds { get; init; }
    public int TotalPatients => PatientIds.Count;
    public int TotalEncounters => EncounterIds.Count;
}
