namespace Dialysis.HIS.Persistence;

internal static class HisSeed
{
    internal static readonly Guid AdminRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    internal static readonly Guid NurseRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

    internal static readonly Guid SchedulingResourceRoomDialysis1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb001");
    internal static readonly Guid SchedulingResourceEquipmentScale = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb002");
    internal static readonly Guid SchedulingResourceStaffNurseSlot = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb003");

    internal static readonly Guid RaDemoOrgMessageId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd01");
    internal static readonly Guid RaDemoQualityTaskId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd02");
    internal static readonly Guid RaDemoFinancialLinkId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd03");
    internal static readonly Guid RaDemoWaitlistId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd04");
    internal static readonly Guid RaDemoEhrDocId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd05");
    internal static readonly Guid RaDemoAlertId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd06");
    internal static readonly Guid RaDemoDispensingId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd07");
    internal static readonly Guid RaDemoCdsId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd08");
    internal static readonly Guid RaDemoAnalyticsJobId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd09");
    internal static readonly Guid RaDemoFullTextId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd0A");
    internal static readonly Guid RaDemoSecurityMechId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd0b");
    internal static readonly Guid RaDemoPatientId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd0c");
    internal static readonly Guid RaDemoMedicationOrderId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd0d");
    internal static readonly Guid RaDemoSpecialistEncounterId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd0e");
    internal static readonly Guid RaDemoResearchEducationActivityId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd0f");
}
