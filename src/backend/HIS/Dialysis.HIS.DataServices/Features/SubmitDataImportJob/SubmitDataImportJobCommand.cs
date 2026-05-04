using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.DataServices.Features.SubmitDataImportJob;

public sealed record SubmitDataImportJobCommand(string SourceDescription)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DataImportSubmit;
}
