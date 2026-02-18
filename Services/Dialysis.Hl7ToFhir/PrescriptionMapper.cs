using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps Prescription (ORC + OBX) to FHIR ServiceRequest.
/// Therapy modality maps to ServiceRequest.code; UF target, blood flow, UF rate map to extensions.
/// Per FHIR Implementation Guide: Prescription → ServiceRequest + DeviceRequest.
/// </summary>
public static class PrescriptionMapper
{
    private const string SnomedSystem = "http://snomed.info/sct";
    private const string HemodialysisSnomed = "1088001";
    private const string UcumSystem = "http://unitsofmeasure.org";
    private const string PrescriptionProfileUrl = "http://dialysis-pdms.local/fhir/StructureDefinition/dialysis-prescription";

    public static ServiceRequest ToFhirServiceRequest(PrescriptionMappingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        string modalityDisplay = input.Modality ?? "Hemodialysis";
        string modalityCode = MapModalityToSnomed(input.Modality);

        var request = new ServiceRequest
        {
            Status = RequestStatus.Active,
            Intent = RequestIntent.Order,
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding(SnomedSystem, modalityCode, modalityDisplay)
                ],
                Text = modalityDisplay
            },
            Subject = new ResourceReference($"Patient/{input.PatientMrn}"),
            AuthoredOn = input.ReceivedAt?.ToString("o")
        };

        request.Identifier.Add(new Identifier("urn:dialysis:order", input.OrderId));

        if (!string.IsNullOrEmpty(input.OrderingProvider))
            request.Requester = new ResourceReference { Display = input.OrderingProvider };

        if (input.BloodFlowRateMlMin.HasValue)
            request.Extension.Add(BuildQuantityExtension(
                $"{PrescriptionProfileUrl}#blood-flow-rate",
                input.BloodFlowRateMlMin.Value,
                "ml/min"));

        if (input.UfRateMlH.HasValue)
            request.Extension.Add(BuildQuantityExtension(
                $"{PrescriptionProfileUrl}#uf-rate",
                input.UfRateMlH.Value,
                "mL/h"));

        if (input.UfTargetVolumeMl.HasValue)
            request.Extension.Add(BuildQuantityExtension(
                $"{PrescriptionProfileUrl}#uf-target-volume",
                input.UfTargetVolumeMl.Value,
                "mL"));

        return request;
    }

    private static Extension BuildQuantityExtension(string url, decimal value, string unit)
    {
        return new Extension
        {
            Url = url,
            Value = new Quantity
            {
                Value = value,
                Unit = unit,
                System = UcumSystem,
                Code = unit
            }
        };
    }

    private static string MapModalityToSnomed(string? modality)
    {
        if (string.IsNullOrEmpty(modality)) return HemodialysisSnomed;

        return modality.ToUpperInvariant() switch
        {
            "HD" or "HEMODIALYSIS" => HemodialysisSnomed,
            "HDF" or "HEMODIAFILTRATION" => "1088001",
            "HF" or "HEMOFILTRATION" => "1088001",
            "UF" or "ULTRAFILTRATION" => "1088001",
            _ => HemodialysisSnomed
        };
    }
}
