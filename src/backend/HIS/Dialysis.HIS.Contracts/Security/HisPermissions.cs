using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIS.Contracts.Security;

/// <summary>
/// HIS facility-operations permissions. Clinical permissions (patient registration, scheduling, medication ordering,
/// patient portal access, billing export) now live in <c>Dialysis.EHR.Contracts</c>; the HIS module no longer owns those.
/// </summary>
public static class HisPermissions
{
    public const string StaffAssign = "his.operations.staff.assign";
    public const string InventoryMove = "his.operations.inventory.move";
    public const string DataImportSubmit = "his.data.import.submit";
    public const string DataSearch = "his.data.search";
    public const string DataReport = "his.data.report";

    /// <summary>Read metadata for transactional outbox rows (data share / interoperability index; no payload body).</summary>
    public const string DataShareRead = "his.data.share.read";

    public const string DeviceIngest = "his.integration.device.ingest";

    /// <summary>Read extended RA capability modules (Tummers et al. 2021) exposed under <c>reference-architecture/capabilities</c>.</summary>
    public const string RaCapabilitiesRead = "his.ra.capabilities.read";

    /// <summary>Write RA-aligned capability rows (waitlists, alerts, CDS, coordination, analytics export requests).</summary>
    public const string RaCommandsWrite = "his.ra.commands.write";

    public static IReadOnlyList<string> All { get; } =
    [
        StaffAssign,
        InventoryMove,
        DataImportSubmit,
        DataSearch,
        DataReport,
        DataShareRead,
        DeviceIngest,
        RaCapabilitiesRead,
        RaCommandsWrite,
    ];
}

/// <summary>Module-aware catalog so <c>Dialysis.Module.Hosting</c> can resolve HIS permission set generically.</summary>
public sealed class HisPermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "his";

    public IReadOnlyCollection<string> All => HisPermissions.All;
}
