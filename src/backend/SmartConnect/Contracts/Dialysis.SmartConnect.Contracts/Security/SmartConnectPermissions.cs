using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.SmartConnect.Contracts.Security;

/// <summary>
/// SmartConnect operator permissions. Today the SmartConnect host runs anonymous when no JWT authority is
/// configured; when the management API is fronted by an IdP, these strings are the role/group → permission
/// targets mapped via <c>SmartConnect:Authentication:RolePermissionMap</c>.
/// </summary>
public static class SmartConnectPermissions
{
    public const string FlowsRead = "smartconnect.flows.read";
    public const string FlowsWrite = "smartconnect.flows.write";
    public const string FlowsRuntimeControl = "smartconnect.flows.runtime.control";
    public const string GroupsWrite = "smartconnect.groups.write";
    public const string MessagesRead = "smartconnect.messages.read";
    public const string MessagesReprocess = "smartconnect.messages.reprocess";
    public const string ConfigurationMapWrite = "smartconnect.configuration-map.write";
    public const string EventsRead = "smartconnect.events.read";
    public const string PrunerRead = "smartconnect.pruner.read";

    public static IReadOnlyList<string> All { get; } =
    [
        FlowsRead,
        FlowsWrite,
        FlowsRuntimeControl,
        GroupsWrite,
        MessagesRead,
        MessagesReprocess,
        ConfigurationMapWrite,
        EventsRead,
        PrunerRead,
    ];
}

/// <summary>Module-aware catalog so <c>Dialysis.Module.Hosting</c> can resolve the SmartConnect permission set generically.</summary>
public sealed class SmartConnectPermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "smartconnect";

    public IReadOnlyCollection<string> All => SmartConnectPermissions.All;
}
