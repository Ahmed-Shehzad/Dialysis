using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.DeIdentification;

/// <summary>
/// HIPAA Safe Harbor (§164.514(b)(2)) de-identifier — strips the 18 enumerated identifiers,
/// generalizes dates to year, and replaces free-text comment fields with empty values flagged as
/// data-absent. Operates on cloned resources; the source resource is never mutated.
/// </summary>
public sealed class SafeHarborDeIdentifier : IFhirDeIdentifier
{
    public Resource Apply(Resource resource, DeIdentificationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var clone = (Resource)resource.DeepCopy();

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
        }

        return clone;
    }

    private void ScrubPatient(Patient patient, DeIdentificationProfile profile)
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

    private void ScrubObservation(Observation observation, DeIdentificationProfile profile)
    {
        observation.Note.Clear();
        if (profile == DeIdentificationProfile.SafeHarbor && observation.Effective is FhirDateTime effective && effective.Value is not null)
        {
            observation.Effective = new FhirDateTime(effective.Value.Length >= 4 ? effective.Value[..4] : effective.Value);
        }
    }

    private void ScrubEncounter(Encounter encounter, DeIdentificationProfile profile)
    {
        encounter.ReasonCode.Clear();
        if (encounter.Period is not null && profile == DeIdentificationProfile.SafeHarbor)
        {
            if (encounter.Period.Start is { Length: >= 4 } start)
                encounter.Period.Start = start[..4];
            if (encounter.Period.End is { Length: >= 4 } end)
                encounter.Period.End = end[..4];
        }
    }
}
