using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Contracts.Security;

/// <summary>
/// Permissions for the Health Information Exchange module. Cover outbound FHIR publishing,
/// inbound FHIR receipt, consent management, openEHR composition read, and partner administration.
/// </summary>
public static class HiePermissions
{
    public const string OutboundPublish = "hie.outbound.publish";
    public const string InboundReceive = "hie.inbound.receive";
    public const string ConsentManage = "hie.consent.manage";
    public const string OpenEhrRead = "hie.openehr.read";
    public const string PartnersAdminister = "hie.partners.administer";
    public const string DocumentsView = "hie.documents.view";
    public const string DocumentsUpload = "hie.documents.upload";
    public const string DocumentsFill = "hie.documents.fill";
    public const string DocumentsSign = "hie.documents.sign";
    public const string DocumentsDelete = "hie.documents.delete";
    public const string DocumentsRetentionView = "hie.documents.retention.view";
    public const string DocumentsRetentionAdminister = "hie.documents.retention.administer";
    public const string TefcaPartnersView = "hie.tefca.partners.view";
    public const string TefcaPartnersAdminister = "hie.tefca.partners.administer";
    public const string TefcaIasJwtIssue = "hie.tefca.ias_jwt.issue";

    /// <summary>Review the MPI duplicate-match queue and adjudicate link/reject decisions (data steward).</summary>
    public const string MpiStewardReview = "hie.mpi.steward.review";

    /// <summary>View the authored terminology resources (value-set / code-system / concept-map governance).</summary>
    public const string TerminologyView = "hie.terminology.view";

    /// <summary>Author / version / retire terminology resources (terminology governance lead).</summary>
    public const string TerminologyAuthor = "hie.terminology.author";

    public static IReadOnlyList<string> All { get; } =
    [
        OutboundPublish,
        InboundReceive,
        ConsentManage,
        MpiStewardReview,
        TerminologyView,
        TerminologyAuthor,
        OpenEhrRead,
        PartnersAdminister,
        DocumentsView,
        DocumentsUpload,
        DocumentsFill,
        DocumentsSign,
        DocumentsDelete,
        DocumentsRetentionView,
        DocumentsRetentionAdminister,
        TefcaPartnersView,
        TefcaPartnersAdminister,
        TefcaIasJwtIssue,
    ];
}

/// <summary>Module-aware catalog so <c>Dialysis.Module.Hosting</c> can resolve HIE permission set generically.</summary>
public sealed class HiePermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "hie";

    public IReadOnlyCollection<string> All => HiePermissions.All;
}
