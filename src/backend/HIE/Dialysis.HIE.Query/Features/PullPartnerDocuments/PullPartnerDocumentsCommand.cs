using Dialysis.CQRS.Commands;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Query.Features.PullPartnerRecords;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Query.Features.PullPartnerDocuments;

/// <summary>
/// Cross-gateway document pull (XCA): query a partner registry for a patient's documents, retrieve
/// their content, and land them through the inbound ingestion pipeline.
/// </summary>
public sealed record PullPartnerDocumentsCommand : ICommand<PartnerPullResult>, IPermissionedCommand
{
    /// <summary>
    /// Cross-gateway document pull (XCA): query a partner registry for a patient's documents, retrieve
    /// their content, and land them through the inbound ingestion pipeline.
    /// </summary>
    public PullPartnerDocumentsCommand(Guid PartnerId, string PartnerPatientId, string? Purpose = null)
    {
        this.PartnerId = PartnerId;
        this.PartnerPatientId = PartnerPatientId;
        this.Purpose = Purpose;
    }

    public string RequiredPermission => HiePermissions.InboundReceive;
    public Guid PartnerId { get; init; }
    public string PartnerPatientId { get; init; }
    public string? Purpose { get; init; }

    public void Deconstruct(out Guid partnerId, out string partnerPatientId, out string? purpose)
    {
        partnerId = this.PartnerId;
        partnerPatientId = this.PartnerPatientId;
        purpose = this.Purpose;
    }
}
