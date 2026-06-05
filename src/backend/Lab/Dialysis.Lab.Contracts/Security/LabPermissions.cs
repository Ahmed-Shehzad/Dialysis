using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Lab.Contracts.Security;

/// <summary>
/// Closed permission set for the Laboratory bounded context — lab order placement and result review.
/// </summary>
public static class LabPermissions
{
    public const string OrderPlace = "lab.order.place";
    public const string OrderRead = "lab.order.read";
    public const string OrderCancel = "lab.order.cancel";
    public const string ResultRead = "lab.result.read";

    public static IReadOnlyList<string> All { get; } =
    [
        OrderPlace,
        OrderRead,
        OrderCancel,
        ResultRead,
    ];
}

/// <summary>Module permission catalog consumed by the Lab host's <c>AddModuleHost</c> registration.</summary>
public sealed class LabPermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "lab";

    public IReadOnlyCollection<string> All => LabPermissions.All;
}
