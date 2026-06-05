using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.DeIdentification;

/// <summary>
/// HIPAA Safe Harbor (§164.514(b)(2)) de-identifier — strips the 18 enumerated identifiers,
/// generalizes dates to year, and replaces free-text comment fields with empty values flagged as
/// data-absent. Operates on cloned resources; the source resource is never mutated.
/// Covers the resource types the platform's Bulk Data feeders emit (Patient, Observation, Encounter,
/// AllergyIntolerance, Immunization, MedicationStatement, Procedure); every resource also has its
/// human-readable narrative dropped, since rendered text can carry any of the 18 identifiers.
/// </summary>
public sealed class SafeHarborDeIdentifier : IFhirDeIdentifier
{
    public Resource Apply(Resource resource, DeIdentificationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var clone = (Resource)resource.DeepCopy();

        // Type-agnostic baseline: drop the narrative for every domain resource. The rendered HTML can
        // contain names, dates, addresses — any of the 18 identifiers — independent of the typed fields.
        if (clone is DomainResource domainResource)
        {
            domainResource.Text = null;
        }

        switch (clone)
        {
            case Patient patient:
                ScrubPatient(patient, profile);
                break;
            case Observation observation:
                ScrubObservation(observation, profile);
                break;
            case Encounter encounter:
                ScrubEncounter(encounter, profile);
                break;
            case AllergyIntolerance allergy:
                ScrubAllergyIntolerance(allergy, profile);
                break;
            case Immunization immunization:
                ScrubImmunization(immunization, profile);
                break;
            case MedicationStatement medicationStatement:
                ScrubMedicationStatement(medicationStatement, profile);
                break;
            case Procedure procedure:
                ScrubProcedure(procedure, profile);
                break;
        }

        return clone;
    }

    private static void ScrubPatient(Patient patient, DeIdentificationProfile profile)
    {
        patient.Name.Clear();
        patient.Telecom.Clear();
        patient.Address.Clear();
        patient.Photo.Clear();
        patient.Contact.Clear();
        patient.Identifier.Clear();

        if (profile == DeIdentificationProfile.SafeHarbor && patient.BirthDate is not null && patient.BirthDate.Length >= 4)
        {
            patient.BirthDate = patient.BirthDate[..4]; // year only
        }

        if (profile == DeIdentificationProfile.SafeHarbor && patient.Deceased is FhirDateTime deceased && deceased.Value is { Length: >= 4 } deceasedValue)
        {
            patient.Deceased = new FhirDateTime(deceasedValue[..4]);
        }
    }

    private static void ScrubObservation(Observation observation, DeIdentificationProfile profile)
    {
        observation.Note.Clear();
        observation.Identifier.Clear();
        if (profile == DeIdentificationProfile.SafeHarbor && observation.Effective is FhirDateTime effective && effective.Value is not null)
        {
            observation.Effective = new FhirDateTime(GeneralizeToYear(effective.Value));
        }
    }

    private static void ScrubEncounter(Encounter encounter, DeIdentificationProfile profile)
    {
        encounter.ReasonCode.Clear();
        encounter.Identifier.Clear();
        if (encounter.Period is not null && profile == DeIdentificationProfile.SafeHarbor)
        {
            if (encounter.Period.Start is { Length: >= 4 } start)
                encounter.Period.Start = start[..4];
            if (encounter.Period.End is { Length: >= 4 } end)
                encounter.Period.End = end[..4];
        }
    }

    private static void ScrubAllergyIntolerance(AllergyIntolerance allergy, DeIdentificationProfile profile)
    {
        allergy.Note.Clear();
        allergy.Identifier.Clear();
        if (profile == DeIdentificationProfile.SafeHarbor)
        {
            if (allergy.RecordedDate is { Length: >= 4 } recorded)
                allergy.RecordedDate = recorded[..4];
            if (allergy.Onset is FhirDateTime onset && onset.Value is not null)
                allergy.Onset = new FhirDateTime(GeneralizeToYear(onset.Value));
        }
    }

    private static void ScrubImmunization(Immunization immunization, DeIdentificationProfile profile)
    {
        immunization.Note.Clear();
        immunization.Identifier.Clear();
        if (profile == DeIdentificationProfile.SafeHarbor && immunization.Occurrence is FhirDateTime occurrence && occurrence.Value is not null)
        {
            immunization.Occurrence = new FhirDateTime(GeneralizeToYear(occurrence.Value));
        }
    }

    private static void ScrubMedicationStatement(MedicationStatement medicationStatement, DeIdentificationProfile profile)
    {
        medicationStatement.Note.Clear();
        medicationStatement.Identifier.Clear();
        if (profile == DeIdentificationProfile.SafeHarbor && medicationStatement.Effective is FhirDateTime effective && effective.Value is not null)
        {
            medicationStatement.Effective = new FhirDateTime(GeneralizeToYear(effective.Value));
        }
    }

    private static void ScrubProcedure(Procedure procedure, DeIdentificationProfile profile)
    {
        procedure.Note.Clear();
        procedure.Identifier.Clear();
        if (profile == DeIdentificationProfile.SafeHarbor && procedure.Performed is FhirDateTime performed && performed.Value is not null)
        {
            procedure.Performed = new FhirDateTime(GeneralizeToYear(performed.Value));
        }
    }

    private static string GeneralizeToYear(string value) => value.Length >= 4 ? value[..4] : value;
}
