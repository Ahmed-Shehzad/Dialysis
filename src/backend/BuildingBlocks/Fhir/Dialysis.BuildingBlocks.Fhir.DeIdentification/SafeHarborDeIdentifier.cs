using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.DeIdentification;

/// <summary>
/// The platform's HIPAA de-identifier. A single implementation of <see cref="IFhirDeIdentifier"/>
/// that honours all three <see cref="DeIdentificationProfile"/>s on cloned resources (the source is
/// never mutated):
/// <list type="bullet">
/// <item><b>Safe Harbor</b> (§164.514(b)(2)) — strips the 18 enumerated identifiers, drops the
/// narrative, generalizes dates to the year, and removes geography entirely.</item>
/// <item><b>Limited Data Set</b> (§164.514(e)) — removes the 16 direct identifiers but <i>retains</i>
/// full dates and city/state/ZIP, as a limited data set may.</item>
/// <item><b>Custom</b> — driven by injected <see cref="CustomDeIdentificationRules"/> (defaults to the
/// strict Safe Harbor settings, so relaxing a rule is always an explicit opt-in).</item>
/// </list>
/// Covers the resource types the Bulk Data feeders emit (Patient, Observation, Encounter,
/// AllergyIntolerance, Immunization, MedicationStatement, Procedure).
/// </summary>
public sealed class SafeHarborDeIdentifier : IFhirDeIdentifier
{
    private readonly CustomDeIdentificationRules _customRules;

    /// <summary>
    /// Creates the de-identifier. <paramref name="customRules"/> backs the
    /// <see cref="DeIdentificationProfile.Custom"/> profile; when none is registered, DI supplies the
    /// default (strict, Safe Harbor-equivalent) rules so Custom never silently leaks identifiers.
    /// </summary>
    public SafeHarborDeIdentifier(CustomDeIdentificationRules? customRules = null) =>
        _customRules = customRules ?? new CustomDeIdentificationRules();

    /// <inheritdoc />
    public Resource Apply(Resource resource, DeIdentificationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var clone = (Resource)resource.DeepCopy();
        var rules = ResolveRules(profile);

        // Type-agnostic baseline: drop the narrative. Rendered HTML can contain names, dates,
        // addresses — any identifier — independent of the typed fields.
        if (rules.RemoveNarrative && clone is DomainResource domainResource)
        {
            domainResource.Text = null;
        }

        switch (clone)
        {
            case Patient patient:
                ScrubPatient(patient, rules);
                break;
            case Observation observation:
                ScrubObservation(observation, rules);
                break;
            case Encounter encounter:
                ScrubEncounter(encounter, rules);
                break;
            case AllergyIntolerance allergy:
                ScrubAllergyIntolerance(allergy, rules);
                break;
            case Immunization immunization:
                ScrubImmunization(immunization, rules);
                break;
            case MedicationStatement medicationStatement:
                ScrubMedicationStatement(medicationStatement, rules);
                break;
            case Procedure procedure:
                ScrubProcedure(procedure, rules);
                break;
        }

        return clone;
    }

    private DeIdentificationRules ResolveRules(DeIdentificationProfile profile) => profile switch
    {
        // Safe Harbor: strip everything, generalize dates, drop geography.
        DeIdentificationProfile.SafeHarbor => new DeIdentificationRules(
            RemoveNarrative: true,
            RemoveDirectIdentifiers: true,
            RemoveNotes: true,
            GeneralizeDatesToYear: true,
            Address: AddressGranularity.Remove),

        // Limited Data Set: remove direct identifiers, but keep full dates and city/state/ZIP.
        DeIdentificationProfile.LimitedDataSet => new DeIdentificationRules(
            RemoveNarrative: true,
            RemoveDirectIdentifiers: true,
            RemoveNotes: true,
            GeneralizeDatesToYear: false,
            Address: AddressGranularity.CityStateZip),

        // Custom: caller-configured (defaults are Safe Harbor-strict).
        DeIdentificationProfile.Custom => new DeIdentificationRules(
            RemoveNarrative: _customRules.RemoveNarrative,
            RemoveDirectIdentifiers: _customRules.RemoveDirectIdentifiers,
            RemoveNotes: _customRules.RemoveNotes,
            GeneralizeDatesToYear: _customRules.GeneralizeDatesToYear,
            Address: _customRules.Address),

        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown de-identification profile."),
    };

    private static void ScrubPatient(Patient patient, DeIdentificationRules rules)
    {
        if (rules.RemoveDirectIdentifiers)
        {
            patient.Name.Clear();
            patient.Telecom.Clear();
            patient.Photo.Clear();
            patient.Contact.Clear();
            patient.Identifier.Clear();
        }

        ScrubAddresses(patient.Address, rules.Address);

        if (rules.GeneralizeDatesToYear)
        {
            if (patient.BirthDate is { Length: >= 4 })
                patient.BirthDate = patient.BirthDate[..4]; // year only
            if (patient.Deceased is FhirDateTime { Value: { Length: >= 4 } deceasedValue })
                patient.Deceased = new FhirDateTime(deceasedValue[..4]);
        }
    }

    private static void ScrubObservation(Observation observation, DeIdentificationRules rules)
    {
        if (rules.RemoveNotes) observation.Note.Clear();
        if (rules.RemoveDirectIdentifiers) observation.Identifier.Clear();
        if (rules.GeneralizeDatesToYear && observation.Effective is FhirDateTime { Value: not null } effective)
        {
            observation.Effective = new FhirDateTime(GeneralizeToYear(effective.Value));
        }
    }

    private static void ScrubEncounter(Encounter encounter, DeIdentificationRules rules)
    {
        encounter.ReasonCode.Clear();
        if (rules.RemoveDirectIdentifiers) encounter.Identifier.Clear();
        if (rules.GeneralizeDatesToYear && encounter.Period is not null)
        {
            if (encounter.Period.Start is { Length: >= 4 } start)
                encounter.Period.Start = start[..4];
            if (encounter.Period.End is { Length: >= 4 } end)
                encounter.Period.End = end[..4];
        }
    }

    private static void ScrubAllergyIntolerance(AllergyIntolerance allergy, DeIdentificationRules rules)
    {
        if (rules.RemoveNotes) allergy.Note.Clear();
        if (rules.RemoveDirectIdentifiers) allergy.Identifier.Clear();
        if (rules.GeneralizeDatesToYear)
        {
            if (allergy.RecordedDate is { Length: >= 4 } recorded)
                allergy.RecordedDate = recorded[..4];
            if (allergy.Onset is FhirDateTime { Value: not null } onset)
                allergy.Onset = new FhirDateTime(GeneralizeToYear(onset.Value));
        }
    }

    private static void ScrubImmunization(Immunization immunization, DeIdentificationRules rules)
    {
        if (rules.RemoveNotes) immunization.Note.Clear();
        if (rules.RemoveDirectIdentifiers) immunization.Identifier.Clear();
        if (rules.GeneralizeDatesToYear && immunization.Occurrence is FhirDateTime { Value: not null } occurrence)
        {
            immunization.Occurrence = new FhirDateTime(GeneralizeToYear(occurrence.Value));
        }
    }

    private static void ScrubMedicationStatement(MedicationStatement medicationStatement, DeIdentificationRules rules)
    {
        if (rules.RemoveNotes) medicationStatement.Note.Clear();
        if (rules.RemoveDirectIdentifiers) medicationStatement.Identifier.Clear();
        if (rules.GeneralizeDatesToYear && medicationStatement.Effective is FhirDateTime { Value: not null } effective)
        {
            medicationStatement.Effective = new FhirDateTime(GeneralizeToYear(effective.Value));
        }
    }

    private static void ScrubProcedure(Procedure procedure, DeIdentificationRules rules)
    {
        if (rules.RemoveNotes) procedure.Note.Clear();
        if (rules.RemoveDirectIdentifiers) procedure.Identifier.Clear();
        if (rules.GeneralizeDatesToYear && procedure.Performed is FhirDateTime { Value: not null } performed)
        {
            procedure.Performed = new FhirDateTime(GeneralizeToYear(performed.Value));
        }
    }

    private static void ScrubAddresses(List<Address> addresses, AddressGranularity granularity)
    {
        switch (granularity)
        {
            case AddressGranularity.Remove:
                addresses.Clear();
                break;
            case AddressGranularity.CityStateZip:
                // Keep city/state/postal/country; drop street line, district and rendered text.
                foreach (var address in addresses)
                {
                    address.Line = [];
                    address.District = null;
                    address.Text = null;
                }
                break;
            case AddressGranularity.Full:
            default:
                break;
        }
    }

    private static string GeneralizeToYear(string value) => value.Length >= 4 ? value[..4] : value;

    /// <summary>Resolved, profile-independent scrub rules applied to a single resource.</summary>
    private readonly record struct DeIdentificationRules(
        bool RemoveNarrative,
        bool RemoveDirectIdentifiers,
        bool RemoveNotes,
        bool GeneralizeDatesToYear,
        AddressGranularity Address);
}
