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

    public static IReadOnlyList<string> All { get; } =
    [
        OutboundPublish,
        InboundReceive,
        ConsentManage,
        OpenEhrRead,
        PartnersAdminister,
        DocumentsView,
        DocumentsUpload,
        DocumentsFill,
        DocumentsSign,
        DocumentsDelete,
        DocumentsRetentionView,
        DocumentsRetentionAdminister,
    ];
}

/// <summary>Module-aware catalog so <c>Dialysis.Module.Hosting</c> can resolve HIE permission set generically.</summary>
public sealed class HiePermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "hie";

    public IReadOnlyCollection<string> All => HiePermissions.All;
}
