using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Hl7.Fhir.Model;

namespace Dialysis.PDMS.TreatmentSessions.Fhir;

/// <summary>
/// Streams every PDMS <see cref="DialysisSession"/> as a FHIR R4 <c>Procedure</c> resource for
/// inclusion in a Bulk Data <c>$export</c>. Honours the job's <c>_since</c> filter against the
/// latest of actual/scheduled start timestamps. Status maps PDMS lifecycle to the FHIR
/// EventStatus value set; the procedure code is fixed to SNOMED 302497006 (Haemodialysis).
/// </summary>
public sealed class PdmsDialysisSessionProcedureFeeder : INdjsonResourceFeeder<Procedure>
{
    private readonly IDialysisSessionRepository _sessions;
    /// <summary>
    /// Streams every PDMS <see cref="DialysisSession"/> as a FHIR R4 <c>Procedure</c> resource for
    /// inclusion in a Bulk Data <c>$export</c>. Honours the job's <c>_since</c> filter against the
    /// latest of actual/scheduled start timestamps. Status maps PDMS lifecycle to the FHIR
    /// EventStatus value set; the procedure code is fixed to SNOMED 302497006 (Haemodialysis).
    /// </summary>
    public PdmsDialysisSessionProcedureFeeder(IDialysisSessionRepository sessions) => _sessions = sessions;
    private const string HemodialysisSnomed = "302497006";
    private const string SnomedSystem = "http://snomed.info/sct";

    public IAsyncEnumerable<Procedure> StreamAsync(ExportJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        return StreamCoreAsync(job, cancellationToken);
    }

    private async IAsyncEnumerable<Procedure> StreamCoreAsync(
        ExportJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var session in _sessions.StreamAllAsync(job.Since, cancellationToken).ConfigureAwait(false))
        {
            yield return Project(session);
        }
    }

    private static Procedure Project(DialysisSession source)
    {
        var procedure = new Procedure
        {
            Id = source.Id.ToString(),
            Status = MapStatus(source.Status),
            Subject = new ResourceReference($"Patient/{source.PatientId}"),
            Code = new CodeableConcept(SnomedSystem, HemodialysisSnomed, "Haemodialysis"),
            Performed = BuildPerformedPeriod(source),
        };

        if (source.AchievedUfVolumeLiters is { } liters)
        {
            procedure.Note =
            [
                new Annotation { Text = new Markdown($"Achieved UF: {liters:0.##} L") },
            ];
        }

        if (!string.IsNullOrWhiteSpace(source.AbortReasonCode))
        {
            procedure.StatusReason = new CodeableConcept(SnomedSystem, source.AbortReasonCode);
        }

        return procedure;
    }

    private static Period BuildPerformedPeriod(DialysisSession source) => new()
    {
        StartElement = new FhirDateTime(source.ActualStartUtc ?? source.ScheduledStartUtc),
        EndElement = source.ActualEndUtc is { } end ? new FhirDateTime(end) : null,
    };

    private static EventStatus MapStatus(DialysisSessionStatus status) =>
        status switch
        {
            DialysisSessionStatus.Scheduled => EventStatus.Preparation,
            DialysisSessionStatus.InProgress => EventStatus.InProgress,
            DialysisSessionStatus.Paused => EventStatus.OnHold,
            DialysisSessionStatus.Completed => EventStatus.Completed,
            DialysisSessionStatus.Aborted => EventStatus.Stopped,
            DialysisSessionStatus.Cancelled => EventStatus.NotDone,
            _ => EventStatus.Unknown,
        };
}
