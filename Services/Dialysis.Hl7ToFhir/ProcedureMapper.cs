using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps treatment session metadata to FHIR Procedure.
/// Session status maps to Procedure.status: Active → in-progress, Completed → completed.
/// </summary>
public static class ProcedureMapper
{
    private const string SnomedSystem = "http://snomed.info/sct";
    private const string HemodialysisSnomed = "1088001";

    /// <summary>
    /// Create a FHIR Procedure for a dialysis session.
    /// </summary>
    /// <param name="sessionId">Therapy/session identifier.</param>
    /// <param name="patientId">Patient reference ID.</param>
    /// <param name="deviceId">Device reference ID.</param>
    /// <param name="status">Session status: Active, Completed.</param>
    /// <param name="startedAt">Session start time.</param>
    /// <param name="endedAt">Session end time.</param>
    public static Procedure ToFhirProcedure(
        string sessionId,
        string? patientId,
        string? deviceId,
        string status,
        DateTimeOffset? startedAt,
        DateTimeOffset? endedAt)
    {
        EventStatus fhirStatus = MapStatusToEventStatus(status);

        var proc = new Procedure
        {
            Status = fhirStatus,
            Code = new CodeableConcept
            {
                Coding = [new Coding(SnomedSystem, HemodialysisSnomed, "Hemodialysis")],
                Text = "Hemodialysis"
            },
            Performed = BuildPerformedPeriod(startedAt, endedAt),
            Subject = new ResourceReference(string.IsNullOrEmpty(patientId) ? "Patient/unknown" : $"Patient/{patientId}")
        };

        proc.Identifier.Add(new Identifier("urn:dialysis:session", sessionId));

        if (!string.IsNullOrEmpty(deviceId))
            proc.Performer.Add(new Procedure.PerformerComponent
            {
                Actor = new ResourceReference($"Device/{deviceId}")
            });

        return proc;
    }

    private static EventStatus MapStatusToEventStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return EventStatus.Completed;

        return status.ToUpperInvariant() switch
        {
            "ACTIVE" => EventStatus.InProgress,
            "COMPLETED" => EventStatus.Completed,
            "PREPARATION" or "PREPARED" => EventStatus.Preparation,
            "CANCELLED" => EventStatus.Stopped,
            "STOPPED" => EventStatus.Stopped,
            _ => EventStatus.Completed
        };
    }

    private static Period? BuildPerformedPeriod(DateTimeOffset? startedAt, DateTimeOffset? endedAt)
    {
        if (startedAt.HasValue && endedAt.HasValue)
            return new Period(new FhirDateTime(startedAt.Value), new FhirDateTime(endedAt.Value));

        if (startedAt.HasValue)
            return new Period { StartElement = new FhirDateTime(startedAt.Value) };

        return null;
    }
}
