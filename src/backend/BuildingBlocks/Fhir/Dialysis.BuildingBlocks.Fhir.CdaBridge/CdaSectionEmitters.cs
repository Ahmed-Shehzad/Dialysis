using System.Xml.Linq;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

/// <summary>
/// Builds C-CDA R2.1 <c>&lt;component&gt;&lt;section&gt;</c> elements from FHIR resources — the
/// inverse of <see cref="CdaSectionParsers"/>. Each emitter produces a section whose templateId,
/// LOINC code, and entry shape match what the parser recognises, so a bundle that round-trips
/// through both directions preserves its clinical content.
/// </summary>
internal static class CdaSectionEmitters
{
    private static readonly XNamespace _hl7 = CdaConstants.Hl7;

    private static XElement Section(string templateId, string loinc, string title, params XElement[] entries)
    {
        var section = new XElement(_hl7 + "section",
            new XElement(_hl7 + "templateId", new XAttribute("root", templateId)),
            new XElement(_hl7 + "code",
                new XAttribute("code", loinc),
                new XAttribute("codeSystem", CdaConstants.LoincOid)),
            new XElement(_hl7 + "title", title),
            new XElement(_hl7 + "text", title));
        section.Add(entries);
        return new XElement(_hl7 + "component", section);
    }

    public static XElement Problems(IEnumerable<Condition> conditions)
    {
        var entries = conditions.Select(c =>
            new XElement(_hl7 + "entry",
                new XElement(_hl7 + "act",
                    new XElement(_hl7 + "entryRelationship",
                        new XAttribute("typeCode", "SUBJ"),
                        new XElement(_hl7 + "observation",
                            CdaEmitting.CodeElement("value", c.Code),
                            CdaEmitting.EffectiveTimePoint("effectiveTime", c.Onset as DataType))))));
        return Section(CdaConstants.ProblemsTemplateId, CdaConstants.ProblemsSectionLoinc, "Problems", [.. entries]);
    }

    public static XElement Allergies(IEnumerable<AllergyIntolerance> allergies)
    {
        var entries = allergies.Select(a =>
            new XElement(_hl7 + "entry",
                new XElement(_hl7 + "act",
                    new XElement(_hl7 + "entryRelationship",
                        new XAttribute("typeCode", "SUBJ"),
                        new XElement(_hl7 + "observation",
                            CdaEmitting.EffectiveTimePoint("effectiveTime", a.Onset as DataType),
                            new XElement(_hl7 + "participant",
                                new XAttribute("typeCode", "CSM"),
                                new XElement(_hl7 + "participantRole",
                                    new XElement(_hl7 + "playingEntity",
                                        CdaEmitting.CodeElement("code", a.Code)))))))));
        return Section(CdaConstants.AllergiesTemplateId, CdaConstants.AllergiesSectionLoinc, "Allergies", [.. entries]);
    }

    public static XElement Medications(IEnumerable<MedicationStatement> medications)
    {
        var entries = medications.Select(m =>
            new XElement(_hl7 + "entry",
                new XElement(_hl7 + "substanceAdministration",
                    CdaEmitting.EffectiveTimeInterval(m.Effective as Period),
                    new XElement(_hl7 + "consumable",
                        new XElement(_hl7 + "manufacturedProduct",
                            new XElement(_hl7 + "manufacturedMaterial",
                                CdaEmitting.CodeElement("code", m.Medication as CodeableConcept)))))));
        return Section(CdaConstants.MedicationsTemplateId, CdaConstants.MedicationsSectionLoinc, "Medications", [.. entries]);
    }

    public static XElement Observations(
        IEnumerable<Observation> observations, string templateId, string loinc, string title)
    {
        var components = observations.Select(o =>
            new XElement(_hl7 + "component",
                new XElement(_hl7 + "observation",
                    CdaEmitting.CodeElement("code", o.Code),
                    CdaEmitting.EffectiveTimePoint("effectiveTime", o.Effective as DataType),
                    CdaEmitting.ValueElement(o.Value))));
        var entry = new XElement(_hl7 + "entry",
            new XElement(_hl7 + "organizer", [.. components.Cast<object>()]));
        return Section(templateId, loinc, title, entry);
    }

    public static XElement Immunizations(IEnumerable<Immunization> immunizations)
    {
        var entries = immunizations.Select(i =>
        {
            var admin = new XElement(_hl7 + "substanceAdministration",
                new XAttribute("moodCode", "EVN"),
                CdaEmitting.EffectiveTimePoint("effectiveTime", i.Occurrence),
                new XElement(_hl7 + "consumable",
                    new XElement(_hl7 + "manufacturedProduct",
                        new XElement(_hl7 + "manufacturedMaterial",
                            CdaEmitting.CodeElement("code", i.VaccineCode),
                            i.LotNumber is null ? null : new XElement(_hl7 + "lotNumberText", i.LotNumber)))));
            if (i.Status == Immunization.ImmunizationStatusCodes.NotDone)
                admin.SetAttributeValue("negationInd", "true");
            return new XElement(_hl7 + "entry", admin);
        });
        return Section(CdaConstants.ImmunizationsTemplateId, CdaConstants.ImmunizationsSectionLoinc, "Immunizations", [.. entries]);
    }
}
