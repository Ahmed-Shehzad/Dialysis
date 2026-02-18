using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps treatment session metadata to FHIR Procedure.
/// </summary>
public static class ProcedureMapper
{
    /// <summary>
    /// Create a minimal FHIR Procedure for a dialysis session.
    /// </summary>
    public static Procedure ToFhirProcedure(
        string sessionId,
        string? patientId,
        string? deviceId,
        DateTimeOffset? startedAt,
        DateTimeOffset? endedAt)
    {
        var proc = new Procedure
        {
            Status = Hl7.Fhir.Model.EventStatus.Completed,
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding("http://snomed.info/sct", "1088001", "Hemodialysis")
                ]
            },
            Performed = startedAt.HasValue && endedAt.HasValue
                ? new Period(new FhirDateTime(startedAt.Value), new FhirDateTime(endedAt.Value))
                : startedAt.HasValue
                    ? new Period { StartElement = new FhirDateTime(startedAt.Value) }
                    : null
        };

        if (!string.IsNullOrEmpty(patientId))
            proc.Subject = new ResourceReference($"Patient/{patientId}");

        proc.Identifier.Add(new Identifier("urn:dialysis:session", sessionId));

        if (!string.IsNullOrEmpty(deviceId))
            proc.Performer.Add(new Procedure.PerformerComponent
            {
                Actor = new ResourceReference($"Device/{deviceId}")
            });

        return proc;
    }
}
