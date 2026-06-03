using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

/// <summary>
/// C-CDA R2.1 / CCD → FHIR R4 translator. Reads the document-level patient (recordTarget) with
/// full demographics, then walks the structured body's clinical sections — Problems, Allergies,
/// Medications, Results, Vital Signs, Immunizations — emitting the corresponding FHIR resources
/// into a Document bundle anchored by a <see cref="Composition"/>.
///
/// The parser is null-soft throughout: a section that's absent, empty, or carries only sparse
/// entries contributes whatever it validly can without aborting the rest of the document. A
/// structurally invalid document (not XML, or no root) surfaces as a <see cref="FormatException"/>.
/// </summary>
public sealed class DefaultCdaToFhirMapper : ICdaToFhirMapper
{
    private static readonly XNamespace _hl7 = CdaConstants.Hl7;

    public Bundle Map(string cdaXml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cdaXml);

        XDocument doc;
        try
        {
            doc = XDocument.Parse(cdaXml);
        }
        catch (XmlException ex)
        {
            throw new FormatException("CDA document is not well-formed XML.", ex);
        }

        var root = doc.Root ?? throw new FormatException("Empty CDA document.");

        var patient = ExtractPatient(root);
        var sectionRefs = new List<Composition.SectionComponent>();
        var bundle = new Bundle { Type = Bundle.BundleType.Document };

        // Composition is added first (a FHIR Document bundle requires it as entry[0]); its
        // section list is populated as we map each clinical section below.
        var composition = ExtractComposition(root, patient.Id, sectionRefs);
        bundle.Entry.Add(new Bundle.EntryComponent { Resource = composition });
        bundle.Entry.Add(new Bundle.EntryComponent { Resource = patient });

        MapSections(root, patient.Id, bundle, sectionRefs);
        return bundle;
    }

    private static void MapSections(
        XElement root, string patientId, Bundle bundle, List<Composition.SectionComponent> sectionRefs)
    {
        foreach (var section in root.Descendants(_hl7 + "section"))
        {
            var loinc = section.Element(_hl7 + "code")?.Attribute("code")?.Value;
            if (string.IsNullOrEmpty(loinc)) continue;

            var resources = loinc switch
            {
                CdaConstants.ProblemsSectionLoinc =>
                    CdaSectionParsers.ParseProblems(section, patientId).Cast<Resource>(),
                CdaConstants.AllergiesSectionLoinc =>
                    CdaSectionParsers.ParseAllergies(section, patientId).Cast<Resource>(),
                CdaConstants.MedicationsSectionLoinc =>
                    CdaSectionParsers.ParseMedications(section, patientId).Cast<Resource>(),
                CdaConstants.ResultsSectionLoinc =>
                    CdaSectionParsers.ParseObservations(section, patientId, "laboratory").Cast<Resource>(),
                CdaConstants.VitalSignsSectionLoinc =>
                    CdaSectionParsers.ParseObservations(section, patientId, "vital-signs").Cast<Resource>(),
                CdaConstants.ImmunizationsSectionLoinc =>
                    CdaSectionParsers.ParseImmunizations(section, patientId).Cast<Resource>(),
                _ => [],
            };

            var sectionEntry = new Composition.SectionComponent
            {
                Title = section.Element(_hl7 + "title")?.Value,
                Code = CdaParsing.ParseCodeableConcept(section.Element(_hl7 + "code")),
            };
            var anyMapped = false;
            foreach (var resource in resources)
            {
                bundle.Entry.Add(new Bundle.EntryComponent { Resource = resource });
                sectionEntry.Entry.Add(new ResourceReference($"{resource.TypeName}/{resource.Id}"));
                anyMapped = true;
            }
            if (anyMapped) sectionRefs.Add(sectionEntry);
        }
    }

    private static Patient ExtractPatient(XElement root)
    {
        var patientRole = root
            .Descendants(_hl7 + "recordTarget")
            .Descendants(_hl7 + "patientRole")
            .FirstOrDefault();
        var patientElement = patientRole?.Element(_hl7 + "patient");

        var patient = new Patient { Id = Guid.NewGuid().ToString() };

        MapPatientIdentifiers(patientRole, patient);
        MapPatientName(patientElement, patient);
        MapPatientGenderAndBirth(patientElement, patient);
        MapPatientTelecom(patientRole, patient);
        MapPatientAddress(patientRole, patient);
        return patient;
    }

    private static void MapPatientIdentifiers(XElement? patientRole, Patient patient)
    {
        foreach (var id in patientRole?.Elements(_hl7 + "id") ?? [])
        {
            if (CdaParsing.IsNull(id)) continue;
            var rootOid = id.Attribute("root")?.Value;
            var extension = id.Attribute("extension")?.Value;
            if (string.IsNullOrEmpty(extension)) continue;
            patient.Identifier.Add(new Identifier(CdaConstants.OidToUri(rootOid), extension));
        }
        if (patient.Identifier.Count > 0)
            patient.Id = patient.Identifier[0].Value;
    }

    private static void MapPatientName(XElement? patientElement, Patient patient)
    {
        var nameElement = patientElement?.Element(_hl7 + "name");
        if (nameElement is null) return;

        var family = nameElement.Element(_hl7 + "family")?.Value;
        var givens = nameElement.Elements(_hl7 + "given").Select(g => g.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        var prefix = nameElement.Element(_hl7 + "prefix")?.Value;
        var suffix = nameElement.Element(_hl7 + "suffix")?.Value;
        if (string.IsNullOrEmpty(family) && givens.Count == 0) return;

        var humanName = new HumanName { Family = family };
        foreach (var given in givens) humanName.GivenElement.Add(new FhirString(given));
        if (!string.IsNullOrEmpty(prefix)) humanName.PrefixElement.Add(new FhirString(prefix));
        if (!string.IsNullOrEmpty(suffix)) humanName.SuffixElement.Add(new FhirString(suffix));
        patient.Name.Add(humanName);
    }

    private static void MapPatientGenderAndBirth(XElement? patientElement, Patient patient)
    {
        var genderCode = patientElement?.Element(_hl7 + "administrativeGenderCode")?.Attribute("code")?.Value;
        patient.Gender = genderCode switch
        {
            "M" => AdministrativeGender.Male,
            "F" => AdministrativeGender.Female,
            "UN" => AdministrativeGender.Other,
            _ => null,
        };

        var birthTime = patientElement?.Element(_hl7 + "birthTime")?.Attribute("value")?.Value;
        var birthDate = CdaParsing.ParseTimestamp(birthTime);
        if (birthDate is not null) patient.BirthDate = birthDate.Length >= 10 ? birthDate[..10] : birthDate;
    }

    private static void MapPatientTelecom(XElement? patientRole, Patient patient)
    {
        foreach (var telecom in patientRole?.Elements(_hl7 + "telecom") ?? [])
        {
            var value = telecom.Attribute("value")?.Value;
            if (string.IsNullOrWhiteSpace(value)) continue;
            var system = value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                ? ContactPoint.ContactPointSystem.Phone
                : value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                    ? ContactPoint.ContactPointSystem.Email
                    : ContactPoint.ContactPointSystem.Other;
            var stripped = value.Replace("tel:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("mailto:", string.Empty, StringComparison.OrdinalIgnoreCase);
            patient.Telecom.Add(new ContactPoint { System = system, Value = stripped });
        }
    }

    private static void MapPatientAddress(XElement? patientRole, Patient patient)
    {
        var addr = patientRole?.Element(_hl7 + "addr");
        if (CdaParsing.IsNull(addr)) return;

        var address = new Address
        {
            City = addr!.Element(_hl7 + "city")?.Value,
            State = addr.Element(_hl7 + "state")?.Value,
            PostalCode = addr.Element(_hl7 + "postalCode")?.Value,
            Country = addr.Element(_hl7 + "country")?.Value,
        };
        foreach (var line in addr.Elements(_hl7 + "streetAddressLine"))
            if (!string.IsNullOrWhiteSpace(line.Value)) address.LineElement.Add(new FhirString(line.Value));
        var hasContent = address.LineElement.Count > 0 || !string.IsNullOrEmpty(address.City)
            || !string.IsNullOrEmpty(address.State) || !string.IsNullOrEmpty(address.PostalCode);
        if (hasContent) patient.Address.Add(address);
    }

    private static Composition ExtractComposition(
        XElement root, string patientId, List<Composition.SectionComponent> sectionRefs)
    {
        var title = root.Element(_hl7 + "title")?.Value;
        var effectiveTime = root.Element(_hl7 + "effectiveTime")?.Attribute("value")?.Value;
        var date = CdaParsing.ParseTimestamp(effectiveTime);
        return new Composition
        {
            Id = Guid.NewGuid().ToString(),
            Status = CompositionStatus.Final,
            Type = new CodeableConcept
            {
                Coding = [new Coding(CdaConstants.LoincUri, "34133-9", "Summarization of episode note")],
            },
            Subject = new ResourceReference($"Patient/{patientId}"),
            Date = date is { Length: >= 10 } ? date[..10] : date,
            Title = title ?? "Continuity of Care Document",
            Section = sectionRefs,
        };
    }
}
