using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.Reporting.Directory;

/// <summary>
/// A local, denormalized cache of EHR-owned patient demographics (name + MRN + DOB), keyed by the
/// EHR patient id. PDMS does not own patient identity — this projection is fed asynchronously by EHR
/// patient integration events so the <b>background</b> session-report builder can print a real name /
/// MRN on the discharge / shift / billing PDFs without a synchronous cross-module call (the live UI
/// reads EHR directly through the BFF aggregation instead).
/// </summary>
/// <remarks>
/// Modeled as an <see cref="AggregateRoot{TId}"/> purely to reuse the PDMS repository plumbing
/// (<c>IPdmsRepository&lt;,&gt;</c>, in-memory + EF). It raises no events of its own. <see cref="Id"/>
/// is the EHR patient id, so a lookup is <c>GetByIdAsync(session.PatientId)</c>.
/// </remarks>
public sealed class PatientDirectoryEntry : AggregateRoot<Guid>
{
    private PatientDirectoryEntry()
    {
    }

    private PatientDirectoryEntry(Guid patientId) : base(patientId)
    {
    }

    /// <summary>Medical record number (the EHR system-of-record identifier shown to clinicians).</summary>
    public string MedicalRecordNumber { get; private set; } = string.Empty;

    /// <summary>Given (first) name.</summary>
    public string GivenName { get; private set; } = string.Empty;

    /// <summary>Family (last) name.</summary>
    public string FamilyName { get; private set; } = string.Empty;

    /// <summary>Date of birth, when known (demographics-update events don't carry it).</summary>
    public DateOnly? DateOfBirth { get; private set; }

    /// <summary>When this cached entry was last refreshed from an EHR event.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Creates a directory entry for the given EHR patient id.</summary>
    public static PatientDirectoryEntry From(
        Guid patientId, string medicalRecordNumber, string givenName, string familyName,
        DateOnly? dateOfBirth, DateTime updatedAtUtc)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient id required.", nameof(patientId));
        var entry = new PatientDirectoryEntry(patientId);
        entry.Apply(medicalRecordNumber, givenName, familyName, dateOfBirth, updatedAtUtc);
        return entry;
    }

    /// <summary>
    /// Refreshes the cached demographics. A null <paramref name="dateOfBirth"/> keeps the known DOB —
    /// the demographics-update event omits it, and a later update must not erase what registration set.
    /// </summary>
    public void Update(string medicalRecordNumber, string givenName, string familyName,
        DateOnly? dateOfBirth, DateTime updatedAtUtc) =>
        Apply(medicalRecordNumber, givenName, familyName, dateOfBirth ?? DateOfBirth, updatedAtUtc);

    private void Apply(string medicalRecordNumber, string givenName, string familyName,
        DateOnly? dateOfBirth, DateTime updatedAtUtc)
    {
        MedicalRecordNumber = (medicalRecordNumber ?? string.Empty).Trim();
        GivenName = (givenName ?? string.Empty).Trim();
        FamilyName = (familyName ?? string.Empty).Trim();
        DateOfBirth = dateOfBirth;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Given + family name, falling back to the MRN when both name parts are blank.</summary>
    public string DisplayName
    {
        get
        {
            var joined = string.Join(' ',
                new[] { GivenName, FamilyName }.Where(p => !string.IsNullOrWhiteSpace(p)));
            return string.IsNullOrWhiteSpace(joined) ? $"MRN {MedicalRecordNumber}" : joined;
        }
    }
}
