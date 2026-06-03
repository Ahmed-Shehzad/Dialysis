using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

/// <summary>
/// Per-section C-CDA entry parsers. Each method walks the clinical statements of one section and
/// yields the corresponding FHIR R4 resources, all referencing the document's patient. Parsers
/// are deliberately tolerant: an entry missing the coded concept it hangs on is skipped rather
/// than aborting the whole section, so a partner document that mixes conformant and sparse
/// entries still contributes everything it can.
/// </summary>
internal static class CdaSectionParsers
{
    private static readonly XNamespace _hl7 = CdaConstants.Hl7;

    private static CodeableConcept ActiveClinicalStatus(string system, string code) =>
        new() { Coding = [new Coding(system, code, null)] };

    /// <summary>Problem Concern Act → Condition (one per problem observation).</summary>
    public static IEnumerable<Condition> ParseProblems(XElement section, string patientId)
    {
        foreach (var observation in section
            .Descendants(_hl7 + "entry")
            .Descendants(_hl7 + "observation"))
        {
            var code = CdaParsing.ParseCodeableConcept(observation.Element(_hl7 + "value"));
            if (code is null) continue;

            var condition = new Condition
            {
                Id = Guid.NewGuid().ToString(),
                Subject = new ResourceReference($"Patient/{patientId}"),
                Code = code,
                ClinicalStatus = ActiveClinicalStatus(
                    "http://terminology.hl7.org/CodeSystem/condition-clinical", "active"),
            };
            var onset = CdaParsing.ParseEffectiveInstant(observation.Element(_hl7 + "effectiveTime"));
            if (onset is not null) condition.Onset = new FhirDateTime(onset);
            yield return condition;
        }
    }

    /// <summary>Allergy Concern Act → AllergyIntolerance (substance from the participant role).</summary>
    public static IEnumerable<AllergyIntolerance> ParseAllergies(XElement section, string patientId)
    {
        foreach (var observation in section
            .Descendants(_hl7 + "entry")
            .Descendants(_hl7 + "observation"))
        {
            var substance = observation
                .Descendants(_hl7 + "participant")
                .Descendants(_hl7 + "playingEntity")
                .Elements(_hl7 + "code")
                .FirstOrDefault();
            var code = CdaParsing.ParseCodeableConcept(substance);
            if (code is null) continue;

            var allergy = new AllergyIntolerance
            {
                Id = Guid.NewGuid().ToString(),
                Patient = new ResourceReference($"Patient/{patientId}"),
                Code = code,
                ClinicalStatus = ActiveClinicalStatus(
                    "http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical", "active"),
            };
            var onset = CdaParsing.ParseEffectiveInstant(observation.Element(_hl7 + "effectiveTime"));
            if (onset is not null) allergy.Onset = new FhirDateTime(onset);
            yield return allergy;
        }
    }

    /// <summary>Medication Activity → MedicationStatement (product from the consumable).</summary>
    public static IEnumerable<MedicationStatement> ParseMedications(XElement section, string patientId)
    {
        foreach (var admin in section
            .Descendants(_hl7 + "entry")
            .Descendants(_hl7 + "substanceAdministration"))
        {
            var material = admin
                .Descendants(_hl7 + "manufacturedMaterial")
                .Elements(_hl7 + "code")
                .FirstOrDefault();
            var code = CdaParsing.ParseCodeableConcept(material);
            if (code is null) continue;

            var statement = new MedicationStatement
            {
                Id = Guid.NewGuid().ToString(),
                Status = MedicationStatement.MedicationStatusCodes.Active,
                Subject = new ResourceReference($"Patient/{patientId}"),
                Medication = code,
            };
            var period = CdaParsing.ParseEffectivePeriod(admin.Element(_hl7 + "effectiveTime"));
            if (period is not null) statement.Effective = period;
            yield return statement;
        }
    }

    /// <summary>
    /// Results / Vital Signs organizer → Observation (one per component observation). Both
    /// sections share the <c>organizer &gt; component &gt; observation</c> shape; the caller
    /// passes the FHIR observation category that distinguishes them.
    /// </summary>
    public static IEnumerable<Observation> ParseObservations(XElement section, string patientId, string category)
    {
        foreach (var observation in section
            .Descendants(_hl7 + "organizer")
            .Descendants(_hl7 + "component")
            .Elements(_hl7 + "observation"))
        {
            var code = CdaParsing.ParseCodeableConcept(observation.Element(_hl7 + "code"));
            if (code is null) continue;

            var result = new Observation
            {
                Id = Guid.NewGuid().ToString(),
                Status = ObservationStatus.Final,
                Subject = new ResourceReference($"Patient/{patientId}"),
                Code = code,
                Category =
                [
                    new CodeableConcept(
                        "http://terminology.hl7.org/CodeSystem/observation-category", category),
                ],
            };
            AssignObservationValue(observation.Element(_hl7 + "value"), result);
            var when = CdaParsing.ParseEffectiveInstant(observation.Element(_hl7 + "effectiveTime"));
            if (when is not null) result.Effective = new FhirDateTime(when);
            yield return result;
        }
    }

    private static void AssignObservationValue(XElement? value, Observation result)
    {
        if (CdaParsing.IsNull(value)) return;
        var type = CdaParsing.ValueType(value!);
        switch (type)
        {
            case "PQ":
                result.Value = CdaParsing.ParseQuantity(value);
                break;
            case "CD" or "CE" or "CO":
                result.Value = CdaParsing.ParseCodeableConcept(value);
                break;
            default:
                var text = value!.Attribute("value")?.Value ?? value.Value?.Trim();
                if (!string.IsNullOrEmpty(text)) result.Value = new FhirString(text);
                break;
        }
    }

    /// <summary>Immunization Activity → Immunization (vaccine from the consumable).</summary>
    public static IEnumerable<Immunization> ParseImmunizations(XElement section, string patientId)
    {
        foreach (var admin in section
            .Descendants(_hl7 + "entry")
            .Descendants(_hl7 + "substanceAdministration"))
        {
            var material = admin
                .Descendants(_hl7 + "manufacturedMaterial")
                .Elements(_hl7 + "code")
                .FirstOrDefault();
            var vaccine = CdaParsing.ParseCodeableConcept(material);
            if (vaccine is null) continue;

            var notGiven = string.Equals(
                admin.Attribute("negationInd")?.Value, "true", StringComparison.OrdinalIgnoreCase);

            var immunization = new Immunization
            {
                Id = Guid.NewGuid().ToString(),
                Status = notGiven
                    ? Immunization.ImmunizationStatusCodes.NotDone
                    : Immunization.ImmunizationStatusCodes.Completed,
                Patient = new ResourceReference($"Patient/{patientId}"),
                VaccineCode = vaccine,
            };
            var occurred = CdaParsing.ParseEffectiveInstant(admin.Element(_hl7 + "effectiveTime"));
            immunization.Occurrence = occurred is not null
                ? new FhirDateTime(occurred)
                : new FhirString("unknown");
            var lot = admin
                .Descendants(_hl7 + "manufacturedMaterial")
                .Elements(_hl7 + "lotNumberText")
                .FirstOrDefault()?.Value;
            if (!string.IsNullOrWhiteSpace(lot)) immunization.LotNumber = lot;
            yield return immunization;
        }
    }
}
