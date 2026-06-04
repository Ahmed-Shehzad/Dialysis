namespace Dialysis.Module.Contracts.Demo;

/// <summary>
/// One canonical demo patient. Carries only primitives so the catalog stays free of any module's
/// domain types — each module maps these fields onto its own aggregate when seeding.
/// </summary>
public sealed record DemoPatient
{
    /// <summary>
    /// One canonical demo patient. Carries only primitives so the catalog stays free of any module's
    /// domain types — each module maps these fields onto its own aggregate when seeding.
    /// </summary>
    /// <param name="Id">Stable patient id shared across every module's demo data.</param>
    /// <param name="Mrn">Medical record number (e.g. <c>MRN-0001</c>).</param>
    /// <param name="Family">Family (last) name.</param>
    /// <param name="Given">Given (first) name.</param>
    /// <param name="Dob">Date of birth.</param>
    /// <param name="Sex">Administrative sex (<c>female</c> / <c>male</c>).</param>
    /// <param name="Language">Preferred language code (e.g. <c>en-US</c>).</param>
    public DemoPatient(Guid Id,
        string Mrn,
        string Family,
        string Given,
        DateOnly Dob,
        string Sex,
        string Language)
    {
        this.Id = Id;
        this.Mrn = Mrn;
        this.Family = Family;
        this.Given = Given;
        this.Dob = Dob;
        this.Sex = Sex;
        this.Language = Language;
    }

    /// <summary>Stable patient id shared across every module's demo data.</summary>
    public Guid Id { get; init; }

    /// <summary>Medical record number (e.g. <c>MRN-0001</c>).</summary>
    public string Mrn { get; init; }

    /// <summary>Family (last) name.</summary>
    public string Family { get; init; }

    /// <summary>Given (first) name.</summary>
    public string Given { get; init; }

    /// <summary>Date of birth.</summary>
    public DateOnly Dob { get; init; }

    /// <summary>Administrative sex (<c>female</c> / <c>male</c>).</summary>
    public string Sex { get; init; }

    /// <summary>Preferred language code (e.g. <c>en-US</c>).</summary>
    public string Language { get; init; }

    public void Deconstruct(out Guid Id, out string Mrn, out string Family, out string Given, out DateOnly Dob, out string Sex, out string Language)
    {
        Id = this.Id;
        Mrn = this.Mrn;
        Family = this.Family;
        Given = this.Given;
        Dob = this.Dob;
        Sex = this.Sex;
        Language = this.Language;
    }
}

/// <summary>
/// The single source of truth for cross-module demo data. EHR, PDMS, HIE and HIS demo seeders all
/// read from this list so one demo patient flows coherently across modules (same id everywhere) —
/// selecting a patient in EHR surfaces that same patient's PDMS sessions, HIE consents and HIS
/// queue entry. Dev/demo only; module seeders remain gated by their <c>&lt;Module&gt;:Demo:*</c> flags.
/// Lives in the dependency-free <c>Module.Contracts</c> layer so every module (including persistence)
/// can read it without taking a heavier reference.
/// </summary>
public static class DemoDataCatalog
{
    /// <summary>
    /// Canonical demo patients. Ids reuse the well-known <c>…0001</c>..<c>…0005</c> slots PDMS and
    /// HIE already assumed, so they stay deterministic and the existing-data idempotency checks in
    /// each seeder keep working against existing dev databases.
    /// </summary>
    public static readonly IReadOnlyList<DemoPatient> Patients =
    [
        new(new Guid("00000000-0000-0000-0000-000000000001"), "MRN-0001", "Khan",    "Aisha",   new DateOnly(1976, 4, 12),  "female", "en-US"),
        new(new Guid("00000000-0000-0000-0000-000000000002"), "MRN-0002", "Schmidt", "Daniel",  new DateOnly(1962, 9, 30),  "male",   "en-US"),
        new(new Guid("00000000-0000-0000-0000-000000000003"), "MRN-0003", "Okafor",  "Ngozi",   new DateOnly(1989, 1, 7),   "female", "en-US"),
        new(new Guid("00000000-0000-0000-0000-000000000004"), "MRN-0004", "Tanaka",  "Hiroshi", new DateOnly(1955, 11, 18), "male",   "en-US"),
        new(new Guid("00000000-0000-0000-0000-000000000005"), "MRN-0005", "Rivera",  "Sofia",   new DateOnly(1971, 6, 23),  "female", "en-US"),
    ];

    /// <summary>
    /// Stable demo provider id surfaced to the SPA as the authoring provider for notes / encounters.
    /// Distinct from every patient id (it previously collided with patient #1).
    /// </summary>
    public static readonly Guid DemoProviderId = new("00000000-0000-0000-0000-0000000000a1");

    /// <summary>10-digit NPI carrying no real meaning; required to pass provider-registration validation.</summary>
    public const string DemoProviderNpi = "0000000001";

    /// <summary>Demo HIE partner organization id (Cleveland).</summary>
    public const string PartnerCleveland = "partner.cleveland";

    /// <summary>Demo HIE partner organization id (Mayo).</summary>
    public const string PartnerMayo = "partner.mayo";
}
