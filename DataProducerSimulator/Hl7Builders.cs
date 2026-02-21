using System.Globalization;

using Bogus;

namespace Dialysis.DataProducerSimulator;

internal static class Hl7Builders
{
    private const char FieldSep = '|';
    private const char CompSep = '^';

    /// <summary>Builds ORU^R01 with full observation set for React dashboard (charts, context bar, CDS).</summary>
    /// <param name="mrn">Patient medical record number.</param>
    /// <param name="sessionId">Treatment session identifier.</param>
    /// <param name="msgId">HL7 message control ID.</param>
    /// <param name="ts">Message timestamp.</param>
    /// <param name="faker">Bogus faker for random values.</param>
    /// <param name="eventPhase">OBR-12: start, update, or end. Use "end" to complete the session.</param>
    public static string OruR01(string mrn, string sessionId, string msgId, DateTimeOffset ts, Faker faker, string eventPhase = "start")
    {
        string device = $"{faker.Random.AlphaNumeric(6).ToUpperInvariant()}^EUI64^EUI-64";
        int bloodFlow = faker.Random.Int(280, 350);
        int ufRate = faker.Random.Int(400, 600);
        int ufTarget = faker.Random.Int(1500, 2500);
        int ufActual = faker.Random.Int(0, Math.Min(ufTarget, 800));
        int therapyMin = faker.Random.Int(180, 240);
        string ts1 = ts.ToString("yyyyMMddHHmmss");
        string ts2 = ts.AddMinutes(1).ToString("yyyyMMddHHmmss");
        return $"""
            MSH|^~\&|{device}|FAC|PDMS|FAC|{ts1}||ORU^R01^ORU_R01|{msgId}|P|2.6
            PID|||{mrn}^^^^MR
            OBR|1||{sessionId}^MACH^EUI64|||{ts1}||||||{eventPhase}
            OBX|1|NM|MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|{bloodFlow}|ml/min^ml/min^UCUM|||||F|||{ts1}|||AMEAS
            OBX|2|NM|MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC|1.1.3.2|{faker.Random.Int(100, 180)}|mmHg^mm[Hg]^UCUM|80-200||||F|||{ts2}|||AMEAS
            OBX|3|NM|MDC_HDIALY_BLD_PRESS_ART^MDC_HDIALY_BLD_PRESS_ART^MDC|1.1.3.3|{faker.Random.Int(-80, -40)}|mmHg^mm[Hg]^UCUM|||||F|||{ts1}|||AMEAS
            OBX|4|NM|MDC_HDIALY_UF_RATE^MDC_HDIALY_UF_RATE^MDC|1.1.9.1|{ufRate}|ml/h^mL/h^UCUM|||||F|||{ts1}|||AMEAS
            OBX|5|NM|MDC_HDIALY_UF_RATE_SETTING^MDC_HDIALY_UF_RATE_SETTING^MDC|1.1.9.2|{ufRate}|ml/h^mL/h^UCUM|||||F|||{ts1}|||AMEAS
            OBX|6|NM|MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC|1.1.9.3|{ufTarget}|mL^mL^UCUM|||||F|||{ts1}|||AMEAS
            OBX|7|NM|MDC_HDIALY_UF_ACTUAL_REMOVED_VOL^MDC_HDIALY_UF_ACTUAL_REMOVED_VOL^MDC|1.1.9.4|{ufActual}|mL^mL^UCUM|||||F|||{ts1}|||AMEAS
            OBX|8|NM|MDC_PRESS_BLD_SYS^MDC_PRESS_BLD_SYS^MDC|1.1.10.1|{faker.Random.Int(100, 140)}|mmHg^mm[Hg]^UCUM|||||F|||{ts1}|||AMEAS
            OBX|9|NM|MDC_PRESS_BLD_DIA^MDC_PRESS_BLD_DIA^MDC|1.1.10.2|{faker.Random.Int(60, 90)}|mmHg^mm[Hg]^UCUM|||||F|||{ts1}|||AMEAS
            OBX|10|NM|MDC_DIA_THERAPY_TIME_PRES^MDC_DIA_THERAPY_TIME_PRES^MDC|1.1.1.1|{therapyMin}|min^min^UCUM|||||F|||{ts1}|||AMEAS
            """;
    }

    /// <summary>Builds ORU^R40 alarm. OBX-8 uses PH/PM/PL for alarms-by-severity report.</summary>
    public static string OruR40(string mrn, string sessionId, string msgId, DateTimeOffset ts, Faker faker)
    {
        string device = $"{faker.Random.AlphaNumeric(6).ToUpperInvariant()}_EUI64";
        string priority = faker.PickRandom("PH", "PM", "PL");
        return $"""
            MSH|^~\&|{device}|FAC|EMR|FAC|{ts:yyyyMMddHHmmss}||ORU^R40^ORU_R40|{msgId}|P|2.6
            PID|||{mrn}^^^^MR
            OBR|1||{sessionId}^MACH^EUI64
            OBX|1|ST|MDC_EVT_HI_VAL_ALARM^12345^MDC|1.1.3.1.1|MDC_PRESS_BLD_ART^150020^MDC|mmHg
            OBX|2|NM|MDC_PRESS_BLD_ART^12345^MDC|1.1.3.1.2|{faker.Random.Int(60, 200)}|mmHg|||{priority}|||{ts:yyyyMMddHHmmss}
            OBX|3|ST|MDC_ATTR_EVT_PHASE^68481^MDC|1.1.3.1.3|start
            OBX|4|ST|MDC_ATTR_ALARM_STATE^68482^MDC|1.1.3.1.4|active
            OBX|5|ST|MDC_ATTR_ALARM_INACTIVATION_STATE^68483^MDC|1.1.3.1.5|enabled
            """;
    }

    public static string QbpQ22(string mrn, string msgId)
    {
        string ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"MSH|^~\\&|MACH|FAC|EMR|FAC|{ts}||QBP^Q22^QBP_Q21|{msgId}|P|2.6\r\n" +
               $"QPD|IHE PDQ Query^IHE PDQ Query^IHE|Q001|@PID.3^{mrn}^^^^MR\r\n" +
               "RCP|I||RD";
    }

    public static string QbpD01(string mrn, string msgId)
    {
        string ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"MSH|^~\\&|MACH|FAC|EMR|FAC|{ts}||QBP^D01^QBP_D01|{msgId}|P|2.6\r\n" +
               $"QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|{mrn}^^^^MR\r\n" +
               "RCP|I||RD";
    }

    /// <summary>RSP^K22 patient demographics ingest (IHE ITI-21).</summary>
    public static string RspK22Patient(string mrn, string msgId, Faker faker)
    {
        string ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        string lastName = faker.Name.LastName();
        string firstName = faker.Name.FirstName();
        string dob = faker.Date.Past(70, DateTime.UtcNow.AddYears(-18)).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string gender = faker.PickRandom("M", "F");
        return $"""
            MSH{FieldSep}^~\&{FieldSep}EMR{FieldSep}FAC{FieldSep}MACH{FieldSep}FAC{FieldSep}{ts}{FieldSep}{FieldSep}RSP{CompSep}K22{CompSep}RSP_K21{FieldSep}{msgId}{FieldSep}P{FieldSep}2.6
            MSA{FieldSep}AA{FieldSep}{msgId}
            QAK{FieldSep}Q001{FieldSep}OK{FieldSep}IHE PDQ Query{FieldSep}1
            QPD{FieldSep}IHE PDQ Query{FieldSep}Q001{FieldSep}@PID.3.1^{mrn}{CompSep}{CompSep}{CompSep}{CompSep}MR
            PID{FieldSep}{FieldSep}{FieldSep}{mrn}{CompSep}{CompSep}{CompSep}{CompSep}MR{CompSep}{CompSep}{CompSep}{FieldSep}{lastName}{CompSep}{firstName}{FieldSep}{FieldSep}{dob}{FieldSep}{gender}
            """;
    }

    /// <summary>RSP^K22 prescription ingest (OBX: blood flow, UF rate, UF target).</summary>
    public static string RspK22Prescription(string mrn, string orderId, string msgId, Faker faker)
    {
        string ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        int bloodFlow = faker.Random.Int(280, 350);
        int ufRate = faker.Random.Int(400, 600);
        int ufTarget = faker.Random.Int(1500, 2500);
        return $"""
            MSH{FieldSep}^~\&{FieldSep}EMR{FieldSep}FAC{FieldSep}MACH{FieldSep}FAC{FieldSep}{ts}{FieldSep}{FieldSep}RSP{CompSep}K22{CompSep}RSP_K21{FieldSep}{msgId}{FieldSep}P{FieldSep}2.6
            MSA{FieldSep}AA{FieldSep}{msgId}
            QPD{FieldSep}MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC{FieldSep}Q001{FieldSep}@PID.3{FieldSep}{mrn}{CompSep}{CompSep}{CompSep}{CompSep}MR
            ORC{FieldSep}NW{FieldSep}{orderId}^FAC{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{ts}{FieldSep}{FieldSep}{FieldSep}PROVIDER{FieldSep}{FieldSep}
            PID{FieldSep}{FieldSep}{FieldSep}{mrn}{CompSep}{CompSep}{CompSep}{CompSep}MR
            OBX{FieldSep}1{FieldSep}NM{FieldSep}12345^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC{FieldSep}{FieldSep}{bloodFlow}{FieldSep}ml/min{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}RSET
            OBX{FieldSep}2{FieldSep}NM{FieldSep}12346^MDC_HDIALY_UF_RATE_SETTING^MDC{FieldSep}{FieldSep}{ufRate}{FieldSep}mL/h{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}RSET
            OBX{FieldSep}3{FieldSep}NM{FieldSep}12347^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC{FieldSep}{FieldSep}{ufTarget}{FieldSep}mL{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}{FieldSep}RSET
            """;
    }

    /// <summary>Build FHS/BHS/ORU.../BTS/FTS batch from multiple ORU^R01 messages.</summary>
    public static string OruBatch(IReadOnlyList<string> oruMessages)
    {
        var sb = new System.Text.StringBuilder();
        _ = sb.Append("FHS|^~\\&||||||\r\n").Append("BHS|^~\\&||||||\r\n");
        foreach (string oru in oruMessages)
            _ = sb.Append(oru.TrimEnd('\r', '\n')).Append("\r\n");
        _ = sb.Append($"BTS|{oruMessages.Count}\r\n").Append("FTS|1\r\n");
        return sb.ToString();
    }
}
