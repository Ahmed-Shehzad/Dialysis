using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Registration.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.SearchPatients;

public sealed record PatientSummary
{
    public PatientSummary(Guid Id,
        string MedicalRecordNumber,
        string FamilyName,
        string GivenName,
        DateOnly DateOfBirth,
        string? SexAtBirthCode,
        string Status)
    {
        this.Id = Id;
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.DateOfBirth = DateOfBirth;
        this.SexAtBirthCode = SexAtBirthCode;
        this.Status = Status;
    }
    public Guid Id { get; init; }
    public string MedicalRecordNumber { get; init; }
    public string FamilyName { get; init; }
    public string GivenName { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public string? SexAtBirthCode { get; init; }
    public string Status { get; init; }
    public void Deconstruct(out Guid Id, out string MedicalRecordNumber, out string FamilyName, out string GivenName, out DateOnly DateOfBirth, out string? SexAtBirthCode, out string Status)
    {
        Id = this.Id;
        MedicalRecordNumber = this.MedicalRecordNumber;
        FamilyName = this.FamilyName;
        GivenName = this.GivenName;
        DateOfBirth = this.DateOfBirth;
        SexAtBirthCode = this.SexAtBirthCode;
        Status = this.Status;
    }
}

public sealed record PatientSearchResult
{
    public PatientSearchResult(IReadOnlyList<PatientSummary> Items,
        int TotalCount,
        int Skip,
        int Take)
    {
        this.Items = Items;
        this.TotalCount = TotalCount;
        this.Skip = Skip;
        this.Take = Take;
    }
    public IReadOnlyList<PatientSummary> Items { get; init; }
    public int TotalCount { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out IReadOnlyList<PatientSummary> Items, out int TotalCount, out int Skip, out int Take)
    {
        Items = this.Items;
        TotalCount = this.TotalCount;
        Skip = this.Skip;
        Take = this.Take;
    }
}

public sealed record SearchPatientsQuery : IQuery<PatientSearchResult>, IPermissionedCommand
{
    public SearchPatientsQuery(string? Query = null,
        string? FamilyName = null,
        string? GivenName = null,
        string? MedicalRecordNumber = null,
        DateOnly? DateOfBirthFrom = null,
        DateOnly? DateOfBirthTo = null,
        string? SexAtBirthCode = null,
        PatientStatus? Status = null,
        int Skip = 0,
        int Take = 25)
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
    public string RequiredPermission => EhrPermissions.PatientRead;
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
