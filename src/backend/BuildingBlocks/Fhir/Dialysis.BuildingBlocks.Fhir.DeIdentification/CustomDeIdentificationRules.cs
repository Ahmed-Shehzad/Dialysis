namespace Dialysis.BuildingBlocks.Fhir.DeIdentification;

/// <summary>How much of a postal address a de-identification profile retains.</summary>
public enum AddressGranularity
{
    /// <summary>Drop the address entirely (HIPAA Safe Harbor default).</summary>
    Remove,

    /// <summary>Keep city / state / postal code / country; drop street line, district and free text.</summary>
    CityStateZip,

    /// <summary>Keep the address verbatim (only sensible for a Custom profile with a lawful basis).</summary>
    Full,
}

/// <summary>
/// Field rules for the <see cref="DeIdentificationProfile.Custom"/> profile. Defaults are the strict
/// (Safe Harbor-equivalent) settings, so a Custom profile registered with no overrides de-identifies
/// as aggressively as Safe Harbor — relaxing a rule is always an explicit, auditable opt-in.
/// </summary>
public sealed class CustomDeIdentificationRules
{
    /// <summary>Drop the rendered narrative on every resource (it can embed any identifier).</summary>
    public bool RemoveNarrative { get; set; } = true;

    /// <summary>Remove direct identifiers: names, telecom, photos, contacts, business identifiers.</summary>
    public bool RemoveDirectIdentifiers { get; set; } = true;

    /// <summary>Clear free-text note/comment fields (they can embed identifiers).</summary>
    public bool RemoveNotes { get; set; } = true;

    /// <summary>Generalize full dates to the year only (Safe Harbor); off retains the full date (LDS-style).</summary>
    public bool GeneralizeDatesToYear { get; set; } = true;

    /// <summary>How much postal-address detail to retain.</summary>
    public AddressGranularity Address { get; set; } = AddressGranularity.Remove;
}
