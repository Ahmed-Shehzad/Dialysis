using Bogus;

namespace Dialysis.Prescription.Tests;

/// <summary>Overrides for OBX and message metadata in conflict test messages.</summary>
public sealed record RspK22ObxOverrides(
    string Timestamp = "20230215120000",
    string MsgId = "MSG001",
    int BloodFlow = 300,
    int UfRate = 500,
    int UfTarget = 2000,
    string? DialysateObx = null);

/// <summary>Parameters for building RSP^K22 conflict test messages.</summary>
public sealed record RspK22ConflictParams(
    string Mrn,
    string OrderId,
    string CallbackPhone,
    RspK22ObxOverrides? ObxOverrides = null);

/// <summary>
/// Bogus-generated test data for Prescription tests. Uses seed for deterministic results.
/// </summary>
public static class PrescriptionTestData
{
    static PrescriptionTestData() => Randomizer.Seed = new Random(42);

    private static readonly Faker Faker = new();

    public static string Mrn() => Faker.Random.AlphaNumeric(8).ToUpperInvariant();

    public static string OrderId() => $"ORD{Faker.Random.AlphaNumeric(6).ToUpperInvariant()}";

    public static string OrderingProvider() => Faker.Name.FullName();

    public static string CallbackPhone() => Faker.Phone.PhoneNumber("###-####");

    /// <summary>Builds minimal RSP^K22 message with given values. OBX settings use 300, 500, 2000.</summary>
    public static string MinimalRspK22(string mrn, string orderId, string? orderingProvider, string? callbackPhone)
    {
        string orc12 = string.IsNullOrEmpty(orderingProvider) ? "" : $"PROVIDER^{orderingProvider}";
        string orc14 = callbackPhone ?? "";
        return $"""
                MSH|^~\&|EMR|FAC|MACH|FAC|20230215120000||RSP^K22^RSP_K21|MSG001|P|2.6
                MSA|AA|MSG001
                QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|{mrn}^^^^MR
                ORC|NW|{orderId}^FAC|||||20230215120000|||{orc12}||{orc14}
                PID|||{mrn}^^^^MR
                OBX|1|NM|12345^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC||300|ml/min||||||||||RSET
                OBX|2|NM|12346^MDC_HDIALY_UF_RATE_SETTING^MDC||500|mL/h||||||||||RSET
                OBX|3|NM|12347^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC||2000|mL||||||||||RSET
                """;
    }

    /// <summary>Builds QBP^D01 message with given MRN.</summary>
    public static string QbpD01ByMrn(string mrn) =>
        $"""
        MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^D01^QBP_D01|MSG001|P|2.6
        QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|{mrn}^^^^MR
        RCP|I||RD
        """;

    /// <summary>Builds RSP^K22 for conflict tests: same orderId/mrn, configurable phone and OBX values.</summary>
    public static string RspK22ConflictMessage(RspK22ConflictParams p)
    {
        RspK22ObxOverrides obx = p.ObxOverrides ?? new RspK22ObxOverrides();
        string obxLines = $"""
                OBX|1|NM|12345^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC||{obx.BloodFlow}|ml/min||||||||||RSET
                OBX|2|NM|12346^MDC_HDIALY_UF_RATE_SETTING^MDC||{obx.UfRate}|mL/h||||||||||RSET
                OBX|3|NM|12347^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC||{obx.UfTarget}|mL||||||||||RSET
                """;
        if (!string.IsNullOrEmpty(obx.DialysateObx))
            obxLines += $"\n{obx.DialysateObx}";
        return $"""
                MSH|^~\&|EMR|FAC|MACH|FAC|{obx.Timestamp}||RSP^K22^RSP_K21|{obx.MsgId}|P|2.6
                MSA|AA|{obx.MsgId}
                QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|{p.Mrn}^^^^MR
                ORC|NW|{p.OrderId}^FAC|||||{obx.Timestamp}|||PROVIDER||{p.CallbackPhone}
                PID|||{p.Mrn}^^^^MR
                {obxLines}
                """;
    }
}
