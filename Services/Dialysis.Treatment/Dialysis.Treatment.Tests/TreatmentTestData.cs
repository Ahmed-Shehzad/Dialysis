using Bogus;

namespace Dialysis.Treatment.Tests;

/// <summary>
/// Bogus-generated test data for Treatment tests. Uses seed for deterministic results.
/// </summary>
public static class TreatmentTestData
{
    static TreatmentTestData() => Randomizer.Seed = new Random(42);

    private static readonly Faker Faker = new();

    public static string Mrn() => Faker.Random.AlphaNumeric(8).ToUpperInvariant();

    public static string SessionId() => $"THERAPY{Faker.Random.AlphaNumeric(6).ToUpperInvariant()}";

    public static string DeviceEui64() => $"{Faker.Random.AlphaNumeric(8).ToUpperInvariant()}^EUI64^EUI-64";

    /// <summary>Builds ORU^R01 message with given MRN and session ID.</summary>
    public static string OruR01(string mrn, string sessionId, string? deviceEui64 = null)
    {
        string devicePart = deviceEui64 ?? "MACH^EUI64^EUI-64";
        return $"""
            MSH|^~\&|{devicePart}|FAC|PDMS|FAC|20230215120000||ORU^R01^ORU_R01|MSG001|P|2.6
            PID|||{mrn}^^^^MR
            OBR|1||{sessionId}^MACH^EUI64|||20230215120000||||||start
            OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|300|ml/min^ml/min^UCUM|||||F|||20230215120000|||AMEAS
            OBX|2|NM|158776^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC|1.1.3.2|120|mmHg^mm[Hg]^UCUM|80-200||||F|||20230215120100|||AMEAS
            """;
    }
}
