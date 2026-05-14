using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

/// <summary>
/// Starter CDA R2 / CCD → FHIR translator. Reads the document-level Patient (recordTarget) and
/// produces a Composition + Patient bundle. Specific section templates (Allergies, Medications, etc.)
/// can extend this implementation per the C-CDA R2.1 IG.
/// </summary>
public sealed class DefaultCdaToFhirMapper : ICdaToFhirMapper
{
    private static readonly XNamespace _hl7 = "urn:hl7-org:v3";

    public Bundle Map(string cdaXml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cdaXml);
        var doc = XDocument.Parse(cdaXml);
        var root = doc.Root ?? throw new FormatException("Empty CDA document.");

        var patient = ExtractPatient(root);
        var composition = ExtractComposition(root, patient.Id);

        var bundle = new Bundle { Type = Bundle.BundleType.Document };
        bundle.Entry.Add(new Bundle.EntryComponent { Resource = composition });
        bundle.Entry.Add(new Bundle.EntryComponent { Resource = patient });
        return bundle;
    }

    private static Patient ExtractPatient(XElement root)
    {
        var patientRole = root.Descendants(_hl7 + "recordTarget").Descendants(_hl7 + "patientRole").FirstOrDefault();
        var idElement = patientRole?.Element(_hl7 + "id");
        var patientElement = patientRole?.Element(_hl7 + "patient");
        var nameElement = patientElement?.Element(_hl7 + "name");

        var patient = new Patient
        {
            Id = idElement?.Attribute("extension")?.Value ?? Guid.NewGuid().ToString(),
        };
        if (idElement?.Attribute("root")?.Value is { } root4)
        {
            var value = idElement.Attribute("extension")?.Value;
            if (value != null)
            {
                patient.Identifier.Add(new Identifier(system: $"urn:oid:{root4}", value: value));
            }
        }
        var family = nameElement?.Element(_hl7 + "family")?.Value;
        var given = nameElement?.Element(_hl7 + "given")?.Value;
        if (!string.IsNullOrEmpty(family) || !string.IsNullOrEmpty(given))
        {
            var humanName = new HumanName { Family = family };
            if (!string.IsNullOrEmpty(given)) humanName.GivenElement.Add(new FhirString(given));
            patient.Name.Add(humanName);
        }
        return patient;
    }

    private static Composition ExtractComposition(XElement root, string patientId)
    {
        var title = root.Element(_hl7 + "title")?.Value;
        var effectiveTime = root.Element(_hl7 + "effectiveTime")?.Attribute("value")?.Value;
        return new Composition
        {
            Status = CompositionStatus.Final,
            Type = new CodeableConcept
            {
                Coding = [new Coding("http://loinc.org", "34133-9", "Summarization of episode note")],
            },
            Subject = new ResourceReference($"Patient/{patientId}"),
            Date = effectiveTime is { Length: >= 8 } ? $"{effectiveTime[..4]}-{effectiveTime.Substring(4, 2)}-{effectiveTime.Substring(6, 2)}" : null,
            Title = title ?? "Continuity of Care Document",
        };
    }
}
