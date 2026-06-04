using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterEhrDocumentExchange;

public sealed record RegisterEhrDocumentExchangeCommand : ICommand<Guid>, IPermissionedCommand
{
    public RegisterEhrDocumentExchangeCommand(Guid PatientId,
        string DocumentTypeCode,
        string ExternalSystemCode,
        string ExternalUri,
        DateTime? ExchangedAtUtc = null)
    {
        this.PatientId = PatientId;
        this.DocumentTypeCode = DocumentTypeCode;
        this.ExternalSystemCode = ExternalSystemCode;
        this.ExternalUri = ExternalUri;
        this.ExchangedAtUtc = ExchangedAtUtc;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public Guid PatientId { get; init; }
    public string DocumentTypeCode { get; init; }
    public string ExternalSystemCode { get; init; }
    public string ExternalUri { get; init; }
    public DateTime? ExchangedAtUtc { get; init; }
    public void Deconstruct(out Guid PatientId, out string DocumentTypeCode, out string ExternalSystemCode, out string ExternalUri, out DateTime? ExchangedAtUtc)
    {
        PatientId = this.PatientId;
        DocumentTypeCode = this.DocumentTypeCode;
        ExternalSystemCode = this.ExternalSystemCode;
        ExternalUri = this.ExternalUri;
        ExchangedAtUtc = this.ExchangedAtUtc;
    }
}
