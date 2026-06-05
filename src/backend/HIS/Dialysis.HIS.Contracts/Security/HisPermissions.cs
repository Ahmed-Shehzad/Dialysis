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

    /// <summary>Register a device in the RPM device registry.</summary>
    public const string DeviceRegister = "his.integration.device.register";

    /// <summary>Manage a registered device (bind/unbind patient, suspend, retire, recalibrate).</summary>
    public const string DeviceManage = "his.integration.device.manage";

    /// <summary>Read the device registry (list + detail).</summary>
    public const string DeviceRead = "his.integration.device.read";

    /// <summary>Read extended RA capability modules (Tummers et al. 2021) exposed under <c>reference-architecture/capabilities</c>.</summary>
    public const string RaCapabilitiesRead = "his.ra.capabilities.read";

    /// <summary>Write RA-aligned capability rows (waitlists, alerts, CDS, coordination, analytics export requests).</summary>
    public const string RaCommandsWrite = "his.ra.commands.write";

    public const string SecurityManage = "his.security.manage";
    public const string SchedulingBook = "his.scheduling.book";
    public const string PatientFlowAdmit = "his.patientflow.admit";

    /// <summary>Read today's patient queue (receptionist's Front Desk view).</summary>
    public const string PatientFlowQueueRead = "his.patientflow.queue.read";

    /// <summary>Mutate today's patient queue (check-in, assign chair, walk-in).</summary>
    public const string PatientFlowQueueManage = "his.patientflow.queue.manage";

    public const string PatientPortalRead = "his.patientaccess.portal.read";
    public const string MedicationOrderPlace = "his.medication.order.place";

    public static IReadOnlyList<string> All { get; } =
    [
        StaffAssign,
        InventoryMove,
        DataImportSubmit,
        DataSearch,
        DataReport,
        DataShareRead,
        DeviceIngest,
        DeviceRegister,
        DeviceManage,
        DeviceRead,
        RaCapabilitiesRead,
        RaCommandsWrite,
        SecurityManage,
        SchedulingBook,
        PatientFlowAdmit,
        PatientFlowQueueRead,
        PatientFlowQueueManage,
        PatientPortalRead,
        MedicationOrderPlace,
    ];
}

/// <summary>Module-aware catalog so <c>Dialysis.Module.Hosting</c> can resolve HIS permission set generically.</summary>
public sealed class HisPermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "his";

    public IReadOnlyCollection<string> All => HisPermissions.All;
}
