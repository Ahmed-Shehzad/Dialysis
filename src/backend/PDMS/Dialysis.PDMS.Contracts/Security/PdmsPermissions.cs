using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.PDMS.Contracts.Security;

/// <summary>
/// Closed permission set for the PDMS module — dialysis session lifecycle and intradialytic monitoring.
/// </summary>
public static class PdmsPermissions
{
    public const string SessionSchedule = "pdms.session.schedule";
    public const string SessionStart = "pdms.session.start";
    public const string SessionComplete = "pdms.session.complete";
    public const string SessionAbort = "pdms.session.abort";
    public const string SessionRead = "pdms.session.read";

    public const string ReadingRecord = "pdms.reading.record";
    public const string ReadingRead = "pdms.reading.read";

    public const string PrescriptionWrite = "pdms.prescription.write";
    public const string DryWeightAssess = "pdms.dry-weight.assess";

    public const string VascularAccessRecord = "pdms.vascular-access.record";

    public const string AlarmRead = "pdms.alarm.read";
    public const string AlarmAcknowledge = "pdms.alarm.acknowledge";

    public static IReadOnlyList<string> All { get; } =
    [
        SessionSchedule, SessionStart, SessionComplete, SessionAbort, SessionRead,
        ReadingRecord, ReadingRead,
        PrescriptionWrite,
        DryWeightAssess,
        VascularAccessRecord,
        AlarmRead, AlarmAcknowledge,
    ];
}

public sealed class PdmsPermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "pdms";

    public IReadOnlyCollection<string> All => PdmsPermissions.All;
}
