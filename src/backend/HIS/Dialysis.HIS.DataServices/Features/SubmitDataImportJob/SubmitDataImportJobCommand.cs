using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.DataServices.Features.SubmitDataImportJob;

public sealed record SubmitDataImportJobCommand : ICommand<Guid>, IPermissionedCommand
{
    public SubmitDataImportJobCommand(string SourceDescription) => this.SourceDescription = SourceDescription;
    public string RequiredPermission => HisPermissions.DataImportSubmit;
    public string SourceDescription { get; init; }
    public void Deconstruct(out string sourceDescription) => sourceDescription = this.SourceDescription;
}
