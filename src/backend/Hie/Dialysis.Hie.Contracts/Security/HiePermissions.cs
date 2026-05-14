using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Hie.Contracts.Security;

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

    public static IReadOnlyList<string> All { get; } =
    [
        OutboundPublish,
        InboundReceive,
        ConsentManage,
        OpenEhrRead,
        PartnersAdminister,
    ];
}

/// <summary>Module-aware catalog so <c>Dialysis.Module.Hosting</c> can resolve HIE permission set generically.</summary>
public sealed class HiePermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "hie";

    public IReadOnlyCollection<string> All => HiePermissions.All;
}
