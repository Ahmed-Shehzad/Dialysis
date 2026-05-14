using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

/// <summary>
/// Starter FHIR Bundle → C-CDA R2.1 CCD emitter. Produces a minimal ClinicalDocument with header
/// and a recordTarget derived from the bundle's <see cref="Patient"/> entry.
/// </summary>
public sealed class DefaultFhirToCdaMapper : IFhirToCdaMapper
{
    private static readonly XNamespace _hl7 = "urn:hl7-org:v3";

    public string Map(Bundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        var patient = bundle.Entry.Select(e => e.Resource).OfType<Patient>().FirstOrDefault();

        var document = new XElement(_hl7 + "ClinicalDocument",
            new XAttribute(XNamespace.Xmlns + "hl7", _hl7),
            new XElement(_hl7 + "typeId",
                new XAttribute("root", "2.16.840.1.113883.1.3"),
                new XAttribute("extension", "POCD_HD000040")),
            new XElement(_hl7 + "templateId", new XAttribute("root", "2.16.840.1.113883.10.20.22.1.2")),
            new XElement(_hl7 + "code",
                new XAttribute("code", "34133-9"),
                new XAttribute("codeSystem", "2.16.840.1.113883.6.1")),
            new XElement(_hl7 + "title", "Continuity of Care Document"),
            new XElement(_hl7 + "effectiveTime", new XAttribute("value", DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"))));

        if (patient is not null)
        {
            var name = patient.Name.FirstOrDefault();
            document.Add(new XElement(_hl7 + "recordTarget",
                new XElement(_hl7 + "patientRole",
                    new XElement(_hl7 + "id",
                        new XAttribute("extension", patient.Id ?? Guid.NewGuid().ToString()),
                        new XAttribute("root", "urn:dialysis:patient")),
                    new XElement(_hl7 + "patient",
                        new XElement(_hl7 + "name",
                            new XElement(_hl7 + "given", name?.Given?.FirstOrDefault() ?? string.Empty),
                            new XElement(_hl7 + "family", name?.Family ?? string.Empty))))));
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), document).ToString();
    }
}
