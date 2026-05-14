using Dialysis.EHR.Contracts.Integration;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Core.Coding;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Mappers;

public sealed class LabResultMapper : IFhirResourceMapper<LabResultReceivedIntegrationEvent, Observation>
{
    public Observation Map(LabResultReceivedIntegrationEvent e)
    {
        var observation = new Observation
        {
            Id = e.LabResultId.ToString(),
            Status = ObservationStatus.Final,
            Subject = new ResourceReference($"Patient/{e.PatientId}"),
            BasedOn = [new ResourceReference($"ServiceRequest/{e.LabOrderId}")],
            Effective = new FhirDateTime(e.ObservedAtUtc),
            Code = new CodeableConcept(CodeSystems.Loinc, e.LoincCode),
        };

        if (decimal.TryParse(e.ValueText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var numeric))
        {
            observation.Value = new Quantity
            {
                Value = numeric,
                Unit = e.UnitCode,
                System = string.IsNullOrWhiteSpace(e.UnitCode) ? null : CodeSystems.Ucum,
                Code = e.UnitCode,
            };
        }
        else
        {
            observation.Value = new FhirString(e.ValueText);
        }

        if (!string.IsNullOrWhiteSpace(e.ReferenceRangeText))
        {
            observation.ReferenceRange.Add(new Observation.ReferenceRangeComponent
            {
                Text = e.ReferenceRangeText,
            });
        }

        if (!string.IsNullOrWhiteSpace(e.AbnormalFlag) && !string.Equals(e.AbnormalFlag, "N", StringComparison.OrdinalIgnoreCase))
        {
            observation.Interpretation.Add(new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation", e.AbnormalFlag));
        }

        return observation;
    }
}
