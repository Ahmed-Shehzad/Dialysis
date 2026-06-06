using Dialysis.CQRS.Commands;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Query.Features.PullOutsideRecords;

/// <summary>Outcome of an on-demand "pull outside records" action.</summary>
public sealed record OutsideRecordsResult(
    int Candidates, string? ResolvedPartnerPatientId, int RecordsFetched, int DocumentsFetched);

/// <summary>
/// One-click "pull this patient's outside records" for a clinician: resolves the patient at the
/// partner (discovery, when no partner id is supplied), pulls their records (<c>$everything</c>) and
/// documents (XCA), and lands everything through the inbound ingestion pipeline. Purpose-gated.
/// </summary>
public sealed record PullOutsideRecordsCommand : ICommand<OutsideRecordsResult>, IPermissionedCommand
{
    /// <summary>
    /// One-click "pull this patient's outside records": discovery → query → ingest.
    /// </summary>
    public PullOutsideRecordsCommand(
        Guid PartnerId,
        string? PartnerPatientId = null,
        string? Mrn = null,
        string? Family = null,
        string? Given = null,
        DateOnly? BirthDate = null,
        string? Purpose = null)
    {
        this.PartnerId = PartnerId;
        this.PartnerPatientId = PartnerPatientId;
        this.Mrn = Mrn;
        this.Family = Family;
        this.Given = Given;
        this.BirthDate = BirthDate;
        this.Purpose = Purpose;
    }

    public string RequiredPermission => HiePermissions.InboundReceive;
    public Guid PartnerId { get; init; }

    /// <summary>Known partner-side patient id; when null, discovery resolves it from the demographics.</summary>
    public string? PartnerPatientId { get; init; }
    public string? Mrn { get; init; }
    public string? Family { get; init; }
    public string? Given { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? Purpose { get; init; }
}
