using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

/// <summary>
/// FHIR R4 Bundle → C-CDA R2.1 CCD emitter. Produces a ClinicalDocument header + recordTarget
/// from the bundle's <see cref="Patient"/>, then a structured body whose sections round-trip the
/// resources <see cref="DefaultCdaToFhirMapper"/> parses: Problems, Allergies, Medications,
/// Results, Vital Signs, Immunizations. A section is emitted only when the bundle carries at
/// least one resource of that kind.
/// </summary>
public sealed class DefaultFhirToCdaMapper : IFhirToCdaMapper
{
    private static readonly XNamespace _hl7 = CdaConstants.Hl7;

    public string Map(Bundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        var resources = bundle.Entry.Select(e => e.Resource).Where(r => r is not null).ToList();
        var patient = resources.OfType<Patient>().FirstOrDefault();

        var document = new XElement(_hl7 + "ClinicalDocument",
            new XAttribute(XNamespace.Xmlns + "hl7", _hl7.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", CdaConstants.Xsi.NamespaceName),
            new XElement(_hl7 + "typeId",
                new XAttribute("root", "2.16.840.1.113883.1.3"),
                new XAttribute("extension", "POCD_HD000040")),
            new XElement(_hl7 + "templateId", new XAttribute("root", "2.16.840.1.113883.10.20.22.1.2")),
            new XElement(_hl7 + "code",
                new XAttribute("code", "34133-9"),
                new XAttribute("codeSystem", CdaConstants.LoincOid)),
            new XElement(_hl7 + "title", "Continuity of Care Document"),
            new XElement(_hl7 + "effectiveTime",
                new XAttribute("value", DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"))));

        if (patient is not null)
            document.Add(RecordTarget(patient));
        document.Add(StructuredBody(resources, patient?.Id ?? "unknown"));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), document).ToString();
    }

    private static XElement RecordTarget(Patient patient)
    {
        var patientRole = new XElement(_hl7 + "patientRole");

        if (patient.Identifier.Count > 0)
            foreach (var id in patient.Identifier)
                patientRole.Add(new XElement(_hl7 + "id",
                    new XAttribute("extension", id.Value ?? string.Empty),
                    new XAttribute("root", CdaConstants.UriToOid(id.System))));
        else
            patientRole.Add(new XElement(_hl7 + "id",
                new XAttribute("extension", patient.Id ?? Guid.NewGuid().ToString()),
                new XAttribute("root", "urn:dialysis:patient")));

        foreach (var telecom in patient.Telecom)
        {
            var prefix = telecom.System switch
            {
                ContactPoint.ContactPointSystem.Phone => "tel:",
                ContactPoint.ContactPointSystem.Email => "mailto:",
                _ => string.Empty,
            };
            patientRole.Add(new XElement(_hl7 + "telecom",
                new XAttribute("value", $"{prefix}{telecom.Value}")));
        }

        AddAddress(patientRole, patient);
        patientRole.Add(PatientElement(patient));
        return new XElement(_hl7 + "recordTarget", patientRole);
    }

    private static void AddAddress(XElement patientRole, Patient patient)
    {
        var address = patient.Address.FirstOrDefault();
        if (address is null)
            return;
        var addr = new XElement(_hl7 + "addr");
        foreach (var line in address.Line)
            addr.Add(new XElement(_hl7 + "streetAddressLine", line));
        if (!string.IsNullOrEmpty(address.City))
            addr.Add(new XElement(_hl7 + "city", address.City));
        if (!string.IsNullOrEmpty(address.State))
            addr.Add(new XElement(_hl7 + "state", address.State));
        if (!string.IsNullOrEmpty(address.PostalCode))
            addr.Add(new XElement(_hl7 + "postalCode", address.PostalCode));
        if (!string.IsNullOrEmpty(address.Country))
            addr.Add(new XElement(_hl7 + "country", address.Country));
        if (addr.HasElements)
            patientRole.Add(addr);
    }

    private static XElement PatientElement(Patient patient)
    {
        var patientElement = new XElement(_hl7 + "patient");

        var name = patient.Name.FirstOrDefault();
        if (name is not null)
        {
            var nameElement = new XElement(_hl7 + "name");
            foreach (var prefix in name.Prefix)
                nameElement.Add(new XElement(_hl7 + "prefix", prefix));
            foreach (var given in name.Given)
                nameElement.Add(new XElement(_hl7 + "given", given));
            if (!string.IsNullOrEmpty(name.Family))
                nameElement.Add(new XElement(_hl7 + "family", name.Family));
            foreach (var suffix in name.Suffix)
                nameElement.Add(new XElement(_hl7 + "suffix", suffix));
            patientElement.Add(nameElement);
        }

        var genderCode = patient.Gender switch
        {
            AdministrativeGender.Male => "M",
            AdministrativeGender.Female => "F",
            AdministrativeGender.Other => "UN",
            _ => null,
        };
        if (genderCode is not null)
            patientElement.Add(new XElement(_hl7 + "administrativeGenderCode",
                new XAttribute("code", genderCode),
                new XAttribute("codeSystem", "2.16.840.1.113883.5.1")));

        if (!string.IsNullOrEmpty(patient.BirthDate))
            patientElement.Add(new XElement(_hl7 + "birthTime",
                new XAttribute("value", patient.BirthDate.Replace("-", string.Empty, StringComparison.Ordinal))));

        return patientElement;
    }

    private static XElement StructuredBody(IReadOnlyList<Resource?> resources, string patientId)
    {
        _ = patientId;
        var body = new XElement(_hl7 + "structuredBody");

        var conditions = resources.OfType<Condition>().ToList();
        if (conditions.Count > 0)
            body.Add(CdaSectionEmitters.Problems(conditions));

        var allergies = resources.OfType<AllergyIntolerance>().ToList();
        if (allergies.Count > 0)
            body.Add(CdaSectionEmitters.Allergies(allergies));

        var medications = resources.OfType<MedicationStatement>().ToList();
        if (medications.Count > 0)
            body.Add(CdaSectionEmitters.Medications(medications));

        var observations = resources.OfType<Observation>().ToList();
        var labs = observations.Where(o => HasCategory(o, "laboratory")).ToList();
        var vitals = observations.Where(o => HasCategory(o, "vital-signs")).ToList();
        // Observations without a recognised category default to the Results section.
        var uncategorised = observations.Where(o => !HasCategory(o, "laboratory") && !HasCategory(o, "vital-signs")).ToList();
        var results = labs.Concat(uncategorised).ToList();
        if (results.Count > 0)
            body.Add(CdaSectionEmitters.Observations(
                results, CdaConstants.ResultsTemplateId, CdaConstants.ResultsSectionLoinc, "Results"));
        if (vitals.Count > 0)
            body.Add(CdaSectionEmitters.Observations(
                vitals, CdaConstants.VitalSignsTemplateId, CdaConstants.VitalSignsSectionLoinc, "Vital Signs"));

        var immunizations = resources.OfType<Immunization>().ToList();
        if (immunizations.Count > 0)
            body.Add(CdaSectionEmitters.Immunizations(immunizations));

        return new XElement(_hl7 + "component", body);
    }

    private static bool HasCategory(Observation observation, string code) =>
        observation.Category.Exists(c => c.Coding.Exists(coding => coding.Code == code));
}
