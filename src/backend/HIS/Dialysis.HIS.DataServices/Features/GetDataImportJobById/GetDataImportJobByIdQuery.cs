using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.DataServices.Features.GetDataImportJobById;

public sealed record GetDataImportJobByIdQuery(Guid Id)
    : IQuery<DataImportJobStatusDto?>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DataImportSubmit;
}

public sealed record DataImportJobStatusDto(
    Guid Id,
    string SourceDescription,
    DateTime SubmittedAtUtc,
    string StatusCode,
    string? ValidationSummary);
