using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Hl7.Fhir.Model;

namespace Dialysis.EHR.PatientChart.Fhir;

/// <summary>
/// Streams every <c>Allergy</c> aggregate as a FHIR R4 <c>AllergyIntolerance</c>. Severity maps
/// to the FHIR criticality value set; the source coding (typically SNOMED or RxNorm) is forwarded
/// onto <c>AllergyIntolerance.code</c>. The aggregate's <c>UpdatedAtUtc</c> audit timestamp
/// drives <c>Meta.lastUpdated</c> and the incremental (<c>_since</c>) export filter.
/// </summary>
public sealed class EhrAllergyIntoleranceFeeder(IAllergyRepository allergies) : INdjsonResourceFeeder<AllergyIntolerance>
{
    public async IAsyncEnumerable<AllergyIntolerance> StreamAsync(
        ExportJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        await foreach (var allergy in allergies.StreamAllAsync(job.Since, cancellationToken).ConfigureAwait(false))
        {
            yield return new AllergyIntolerance
            {
                Id = allergy.Id.ToString(),
                Meta = new Meta { LastUpdated = allergy.UpdatedAtUtc },
                Patient = new ResourceReference($"Patient/{allergy.PatientId}"),
                Code = new CodeableConcept(allergy.Allergen.System, allergy.Allergen.Code, allergy.Allergen.Display),
                VerificationStatus = MapVerification(allergy.VerificationStatus),
                ClinicalStatus = new CodeableConcept(
                    "http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical",
                    "active"),
                Criticality = MapSeverity(allergy.Severity),
                Onset = allergy.OnsetDate is { } onset ? new FhirDateTime(onset.ToString("yyyy-MM-dd")) : null,
                Reaction = string.IsNullOrWhiteSpace(allergy.ReactionText)
                    ? null
                    : [new AllergyIntolerance.ReactionComponent { Description = allergy.ReactionText }],
            };
        }
    }

    private static AllergyIntolerance.AllergyIntoleranceCriticality? MapSeverity(AllergySeverity severity) =>
        severity switch
        {
            AllergySeverity.Mild => AllergyIntolerance.AllergyIntoleranceCriticality.Low,
            AllergySeverity.Moderate => AllergyIntolerance.AllergyIntoleranceCriticality.Low,
            AllergySeverity.Severe => AllergyIntolerance.AllergyIntoleranceCriticality.High,
            AllergySeverity.LifeThreatening => AllergyIntolerance.AllergyIntoleranceCriticality.High,
            _ => AllergyIntolerance.AllergyIntoleranceCriticality.UnableToAssess,
        };

    private static CodeableConcept MapVerification(AllergyVerificationStatus status) =>
        status switch
        {
            AllergyVerificationStatus.Confirmed => new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", "confirmed"),
            AllergyVerificationStatus.Refuted => new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", "refuted"),
            AllergyVerificationStatus.EnteredInError => new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", "entered-in-error"),
            _ => new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", "unconfirmed"),
        };
}
