using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Hl7.Fhir.Model;
using DomainImmunization = Dialysis.EHR.PatientChart.Domain.Immunization;
using FhirImmunization = Hl7.Fhir.Model.Immunization;

namespace Dialysis.EHR.PatientChart.Fhir;

/// <summary>
/// Streams every <c>Immunization</c> aggregate as a FHIR R4 <c>Immunization</c>. Vaccine code
/// (typically CVX), administered date, lot, manufacturer, and site are forwarded onto the FHIR
/// resource; the administering provider becomes a <c>performer</c> reference.
/// </summary>
public sealed class EhrImmunizationFeeder(IImmunizationRepository immunizations) : INdjsonResourceFeeder<FhirImmunization>
{
    public async IAsyncEnumerable<FhirImmunization> StreamAsync(
        ExportJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        await foreach (var immunization in immunizations.StreamAllAsync(job.Since, cancellationToken).ConfigureAwait(false))
        {
            yield return Project(immunization);
        }
    }

    private static FhirImmunization Project(DomainImmunization source) => new()
    {
        Id = source.Id.ToString(),
        Status = MapStatus(source.Status),
        Patient = new ResourceReference($"Patient/{source.PatientId}"),
        VaccineCode = new CodeableConcept(source.Vaccine.System, source.Vaccine.Code, source.Vaccine.Display),
        Occurrence = new FhirDateTime(source.AdministeredOn.ToString("yyyy-MM-dd")),
        LotNumber = source.LotNumber,
        Manufacturer = string.IsNullOrWhiteSpace(source.Manufacturer)
            ? null
            : new ResourceReference { Display = source.Manufacturer },
        Site = string.IsNullOrWhiteSpace(source.SiteCode)
            ? null
            : new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActSite", source.SiteCode),
        Performer = source.AdministeringProviderId is null
            ? null
            : [new FhirImmunization.PerformerComponent { Actor = new ResourceReference($"Practitioner/{source.AdministeringProviderId}") }],
    };

    private static FhirImmunization.ImmunizationStatusCodes? MapStatus(ImmunizationStatus status) =>
        status switch
        {
            ImmunizationStatus.Completed => FhirImmunization.ImmunizationStatusCodes.Completed,
            ImmunizationStatus.EnteredInError => FhirImmunization.ImmunizationStatusCodes.EnteredInError,
            ImmunizationStatus.NotDone => FhirImmunization.ImmunizationStatusCodes.NotDone,
            _ => null,
        };
}
