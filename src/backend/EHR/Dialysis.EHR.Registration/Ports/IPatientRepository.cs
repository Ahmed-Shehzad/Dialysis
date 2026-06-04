using Dialysis.EHR.Registration.Domain;

namespace Dialysis.EHR.Registration.Ports;

public sealed record PatientSearchCriteria
{
    public PatientSearchCriteria(string? Query,
        string? FamilyName,
        string? GivenName,
        string? MedicalRecordNumber,
        DateOnly? DateOfBirthFrom,
        DateOnly? DateOfBirthTo,
        string? SexAtBirthCode,
        PatientStatus? Status,
        int Skip,
        int Take)
    {
        this.Query = Query;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.DateOfBirthFrom = DateOfBirthFrom;
        this.DateOfBirthTo = DateOfBirthTo;
        this.SexAtBirthCode = SexAtBirthCode;
        this.Status = Status;
        this.Skip = Skip;
        this.Take = Take;
    }
    public string? Query { get; init; }
    public string? FamilyName { get; init; }
    public string? GivenName { get; init; }
    public string? MedicalRecordNumber { get; init; }
    public DateOnly? DateOfBirthFrom { get; init; }
    public DateOnly? DateOfBirthTo { get; init; }
    public string? SexAtBirthCode { get; init; }
    public PatientStatus? Status { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out string? Query, out string? FamilyName, out string? GivenName, out string? MedicalRecordNumber, out DateOnly? DateOfBirthFrom, out DateOnly? DateOfBirthTo, out string? SexAtBirthCode, out PatientStatus? Status, out int Skip, out int Take)
    {
        Query = this.Query;
        FamilyName = this.FamilyName;
        GivenName = this.GivenName;
        MedicalRecordNumber = this.MedicalRecordNumber;
        DateOfBirthFrom = this.DateOfBirthFrom;
        DateOfBirthTo = this.DateOfBirthTo;
        SexAtBirthCode = this.SexAtBirthCode;
        Status = this.Status;
        Skip = this.Skip;
        Take = this.Take;
    }
}

public sealed record PatientSearchPage
{
    public PatientSearchPage(IReadOnlyList<Patient> Items, int TotalCount)
    {
        this.Items = Items;
        this.TotalCount = TotalCount;
    }
    public IReadOnlyList<Patient> Items { get; init; }
    public int TotalCount { get; init; }
    public void Deconstruct(out IReadOnlyList<Patient> Items, out int TotalCount)
    {
        Items = this.Items;
        TotalCount = this.TotalCount;
    }
}

public interface IPatientRepository
{
    Task<Patient?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Patient?> FindByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Patient>> SearchAsync(string? nameFragment, int take, CancellationToken cancellationToken = default);

    Task<PatientSearchPage> SearchAsync(PatientSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams every <see cref="Patient"/> for bulk-export NDJSON output. Ordered by MRN
    /// for stable pagination. The Patient aggregate does not yet carry a last-modified
    /// timestamp, so the FHIR <c>_since</c> filter is best-effort and currently ignored —
    /// the parameter is reserved for the eventual <c>Meta.lastUpdated</c> tracking.
    /// </summary>
    IAsyncEnumerable<Patient> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);

    void Add(Patient patient);
}
