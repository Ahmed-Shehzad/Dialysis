using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterEhrDocumentExchange;

public sealed record RegisterEhrDocumentExchangeCommand(
    Guid PatientId,
    string DocumentTypeCode,
    string ExternalSystemCode,
    string ExternalUri,
    DateTime? ExchangedAtUtc = null)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
