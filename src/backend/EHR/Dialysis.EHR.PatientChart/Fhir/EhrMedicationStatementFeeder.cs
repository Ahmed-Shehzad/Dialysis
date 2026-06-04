using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Hl7.Fhir.Model;
using DomainMedicationStatement = Dialysis.EHR.PatientChart.Domain.MedicationStatement;
using FhirMedicationStatement = Hl7.Fhir.Model.MedicationStatement;

namespace Dialysis.EHR.PatientChart.Fhir;

/// <summary>
/// Streams every <c>MedicationStatement</c> aggregate as a FHIR R4 <c>MedicationStatement</c>.
/// Patient-reported medications carry free-text dose + frequency on a single dosage entry; the
/// period between <c>StartedOn</c> and <c>StoppedOn</c> populates <c>effective[x]</c>. The
/// aggregate's <c>UpdatedAtUtc</c> audit timestamp drives <c>Meta.lastUpdated</c> and the
/// incremental (<c>_since</c>) export filter.
/// </summary>
public sealed class EhrMedicationStatementFeeder : INdjsonResourceFeeder<FhirMedicationStatement>
{
    private readonly IMedicationStatementRepository _statements;
    /// <summary>
    /// Streams every <c>MedicationStatement</c> aggregate as a FHIR R4 <c>MedicationStatement</c>.
    /// Patient-reported medications carry free-text dose + frequency on a single dosage entry; the
    /// period between <c>StartedOn</c> and <c>StoppedOn</c> populates <c>effective[x]</c>. The
    /// aggregate's <c>UpdatedAtUtc</c> audit timestamp drives <c>Meta.lastUpdated</c> and the
    /// incremental (<c>_since</c>) export filter.
    /// </summary>
    public EhrMedicationStatementFeeder(IMedicationStatementRepository statements) => _statements = statements;
    public async IAsyncEnumerable<FhirMedicationStatement> StreamAsync(
        ExportJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        await foreach (var statement in _statements.StreamAllAsync(job.Since, cancellationToken).ConfigureAwait(false))
        {
            yield return Project(statement);
        }
    }

    private static FhirMedicationStatement Project(DomainMedicationStatement source) => new()
    {
        Id = source.Id.ToString(),
        Meta = new Meta { LastUpdated = source.UpdatedAtUtc },
        Status = MapStatus(source.Status),
        Subject = new ResourceReference($"Patient/{source.PatientId}"),
        Medication = new CodeableConcept(source.Medication.System, source.Medication.Code, source.Medication.Display),
        Effective = new Period
        {
            StartElement = new FhirDateTime(source.StartedOn.ToString("yyyy-MM-dd")),
            EndElement = source.StoppedOn is { } stopped ? new FhirDateTime(stopped.ToString("yyyy-MM-dd")) : null,
        },
        Dosage = string.IsNullOrWhiteSpace(source.DoseText) && string.IsNullOrWhiteSpace(source.FrequencyText)
            ? null
            : [
                new Dosage
                {
                    Text = string.Join(" ", new[] { source.DoseText, source.FrequencyText }.Where(s => !string.IsNullOrWhiteSpace(s))),
                },
            ],
        ReasonCode = string.IsNullOrWhiteSpace(source.ReasonText)
            ? null
            : [new CodeableConcept { Text = source.ReasonText }],
    };

    private static FhirMedicationStatement.MedicationStatusCodes? MapStatus(MedicationStatementStatus status) =>
        status switch
        {
            MedicationStatementStatus.Active => FhirMedicationStatement.MedicationStatusCodes.Active,
            MedicationStatementStatus.Completed => FhirMedicationStatement.MedicationStatusCodes.Completed,
            MedicationStatementStatus.Stopped => FhirMedicationStatement.MedicationStatusCodes.Stopped,
            MedicationStatementStatus.OnHold => FhirMedicationStatement.MedicationStatusCodes.OnHold,
            MedicationStatementStatus.EnteredInError => FhirMedicationStatement.MedicationStatusCodes.EnteredInError,
            _ => null,
        };
}
