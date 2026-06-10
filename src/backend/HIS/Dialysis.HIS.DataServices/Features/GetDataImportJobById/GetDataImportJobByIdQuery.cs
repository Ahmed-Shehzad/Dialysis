using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.DataServices.Features.GetDataImportJobById;

public sealed record GetDataImportJobByIdQuery : IQuery<DataImportJobStatusDto?>, IPermissionedCommand
{
    public GetDataImportJobByIdQuery(Guid Id) => this.Id = Id;
    public string RequiredPermission => HisPermissions.DataImportSubmit;
    public Guid Id { get; init; }
    public void Deconstruct(out Guid id) => id = Id;
}

public sealed record DataImportJobStatusDto
{
    public DataImportJobStatusDto(Guid Id,
        string SourceDescription,
        DateTime SubmittedAtUtc,
        string StatusCode,
        string? ValidationSummary)
    {
        this.Id = Id;
        this.SourceDescription = SourceDescription;
        this.SubmittedAtUtc = SubmittedAtUtc;
        this.StatusCode = StatusCode;
        this.ValidationSummary = ValidationSummary;
    }
    public Guid Id { get; init; }
    public string SourceDescription { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public string StatusCode { get; init; }
    public string? ValidationSummary { get; init; }
    public void Deconstruct(out Guid id, out string sourceDescription, out DateTime submittedAtUtc, out string statusCode, out string? validationSummary)
    {
        id = Id;
        sourceDescription = SourceDescription;
        submittedAtUtc = SubmittedAtUtc;
        statusCode = StatusCode;
        validationSummary = ValidationSummary;
    }
}
