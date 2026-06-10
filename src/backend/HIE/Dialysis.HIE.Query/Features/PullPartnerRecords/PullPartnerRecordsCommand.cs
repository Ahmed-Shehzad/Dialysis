using Dialysis.CQRS.Commands;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Query.Features.PullPartnerRecords;

/// <summary>Result of a partner pull: how many resources were fetched and forwarded to ingestion.</summary>
public sealed record PartnerPullResult(int Fetched);

/// <summary>
/// Query-based exchange (pull): fetch records for a patient from a partner QHIN and feed them into
/// the inbound ingestion pipeline (US Core validation → consent → persist → republish). Authenticated
/// with a purpose-scoped TEFCA IAS JWT.
/// </summary>
public sealed record PullPartnerRecordsCommand : ICommand<PartnerPullResult>, IPermissionedCommand
{
    /// <summary>
    /// Query-based exchange (pull): fetch records for a patient from a partner QHIN and feed them into
    /// the inbound ingestion pipeline. Authenticated with a purpose-scoped TEFCA IAS JWT.
    /// </summary>
    public PullPartnerRecordsCommand(Guid PartnerId, string Query, string Subject, string? Purpose = null)
    {
        this.PartnerId = PartnerId;
        this.Query = Query;
        this.Subject = Subject;
        this.Purpose = Purpose;
    }

    public string RequiredPermission => HiePermissions.InboundReceive;

    /// <summary>The QHIN partner aggregate id to query.</summary>
    public Guid PartnerId { get; init; }

    /// <summary>Relative FHIR query, e.g. <c>Patient/123/$everything</c> or <c>Observation?patient=123</c>.</summary>
    public string Query { get; init; }

    /// <summary>IAS JWT subject — the patient the pull is on behalf of (the partner-side patient id).</summary>
    public string Subject { get; init; }

    /// <summary>Optional TEFCA permitted purpose; defaults to Treatment.</summary>
    public string? Purpose { get; init; }

    public void Deconstruct(out Guid partnerId, out string query, out string subject, out string? purpose)
    {
        partnerId = PartnerId;
        query = Query;
        subject = Subject;
        purpose = Purpose;
    }
}
