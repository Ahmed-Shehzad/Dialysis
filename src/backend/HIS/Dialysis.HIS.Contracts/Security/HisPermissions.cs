namespace Dialysis.HIS.Contracts.Security;

public static class HisPermissions
{
    public const string UserRegister = "his.security.users.register";
    public const string RoleAssign = "his.security.roles.assign";
    public const string PatientRegister = "his.patientflow.register";
    public const string PatientAdmit = "his.patientflow.admit";
    public const string PatientDischarge = "his.patientflow.discharge";
    public const string ReferralCreate = "his.patientflow.referral.create";
    public const string AppointmentBook = "his.scheduling.appointment.book";
    public const string SchedulingResourcesRead = "his.scheduling.resources.read";
    public const string MedicationOrderPlace = "his.medication.order.place";
    public const string MedicationOrderDiscontinue = "his.medication.order.discontinue";
    public const string MedicationAdminRecord = "his.medication.admin.record";
    public const string StaffAssign = "his.operations.staff.assign";
    public const string InventoryMove = "his.operations.inventory.move";
    public const string BillingExport = "his.operations.billing.export";
    public const string DataImportSubmit = "his.data.import.submit";
    public const string DataSearch = "his.data.search";
    public const string DataReport = "his.data.report";

    /// <summary>Read metadata for transactional outbox rows (data share / interoperability index; no payload body).</summary>
    public const string DataShareRead = "his.data.share.read";
    public const string PortalRead = "his.patientaccess.portal.read";
    public const string DeviceIngest = "his.integration.device.ingest";

    /// <summary>Read extended RA capability modules (Tummers et al. 2021) exposed under <c>reference-architecture/capabilities</c>.</summary>
    public const string RaCapabilitiesRead = "his.ra.capabilities.read";

    /// <summary>Write RA-aligned capability rows (waitlists, alerts, CDS, coordination, analytics export requests).</summary>
    public const string RaCommandsWrite = "his.ra.commands.write";

    public static IReadOnlyList<string> All { get; } =
    [
        UserRegister,
        RoleAssign,
        PatientRegister,
        PatientAdmit,
        PatientDischarge,
        ReferralCreate,
        AppointmentBook,
        SchedulingResourcesRead,
        MedicationOrderPlace,
        MedicationOrderDiscontinue,
        MedicationAdminRecord,
        StaffAssign,
        InventoryMove,
        BillingExport,
        DataImportSubmit,
        DataSearch,
        DataReport,
        DataShareRead,
        PortalRead,
        DeviceIngest,
        RaCapabilitiesRead,
        RaCommandsWrite,
    ];
}
