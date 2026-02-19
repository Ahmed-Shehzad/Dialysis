using Bogus;

namespace Dialysis.Alarm.Tests;

/// <summary>
/// Bogus-generated test data for Alarm tests. Uses seed for deterministic results.
/// </summary>
public static class AlarmTestData
{
    static AlarmTestData() => Randomizer.Seed = new Random(42);

    private static readonly Faker Faker = new();

    public static string Mrn() => Faker.Random.AlphaNumeric(8).ToUpperInvariant();

    public static string SessionId() => $"THERAPY{Faker.Random.AlphaNumeric(6).ToUpperInvariant()}";

    public static string DeviceSource() => $"{Faker.Random.AlphaNumeric(8).ToUpperInvariant()}_EUI64";

    /// <summary>Builds ORU^R40 alarm message with given MRN and session ID.</summary>
    public static string OruR40(string mrn, string sessionId, string? deviceSource = null)
    {
        string device = deviceSource ?? "MACH_EUI64";
        return $"""
            MSH|^~\&|{device}|FAC|EMR|FAC|20230215120000||ORU^R40^ORU_R40|MSG001|P|2.6
            PID|||{mrn}^^^^MR
            OBR|1||{sessionId}^MACH^EUI64
            OBX|1|ST|MDC_EVT_HI_VAL_ALARM^12345^MDC|1.1.3.1.1|MDC_PRESS_BLD_ART^150020^MDC|mmHg
            OBX|2|NM|MDC_PRESS_BLD_ART^12345^MDC|1.1.3.1.2|180|mmHg|||H|||20230215120000
            OBX|3|ST|MDC_ATTR_EVT_PHASE^68481^MDC|1.1.3.1.3|start
            OBX|4|ST|MDC_ATTR_ALARM_STATE^68482^MDC|1.1.3.1.4|active
            OBX|5|ST|MDC_ATTR_ALARM_INACTIVATION_STATE^68483^MDC|1.1.3.1.5|enabled
            """;
    }
}
