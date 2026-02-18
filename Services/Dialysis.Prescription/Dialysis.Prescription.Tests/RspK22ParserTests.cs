using Dialysis.Prescription.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Prescription.Tests;

public sealed class RspK22ParserTests
{
    private const string MinimalRspK22 = @"MSH|^~\&|EMR|FAC|MACH|FAC|20230215120000||RSP^K22^RSP_K21|MSG001|P|2.6
MSA|AA|MSG001
QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|MRN123^^^^MR
ORC|NW|ORD001^FAC|||||20230215120000|||PROVIDER^John||555-1234
PID|||MRN123^^^^MR
OBX|1|NM|12345^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC||300|ml/min||||||||||RSET
OBX|2|NM|12346^MDC_HDIALY_UF_RATE_SETTING^MDC||500|mL/h||||||||||RSET
OBX|3|NM|12347^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC||2000|mL||||||||||RSET";

    [Fact]
    public void Parse_ExtractsOrderId()
    {
        var result = new RspK22Parser().Parse(MinimalRspK22);
        result.OrderId.ShouldBe("ORD001");
    }

    [Fact]
    public void Parse_ExtractsPatientMrn()
    {
        var result = new RspK22Parser().Parse(MinimalRspK22);
        result.PatientMrn.Value.ShouldBe("MRN123");
    }

    [Fact]
    public void Parse_ExtractsMsaAndQueryInfo()
    {
        var result = new RspK22Parser().Parse(MinimalRspK22);
        result.MsaAcknowledgmentCode.ShouldBe("AA");
        result.MsaControlId.ShouldBe("MSG001");
        result.QueryTag.ShouldBe("Q001");
        result.QpdQueryName.ShouldBe("MDC_HDIALY_RX_QUERY");
    }

    [Fact]
    public void Parse_ExtractsConstantSettings()
    {
        var result = new RspK22Parser().Parse(MinimalRspK22);
        result.Settings.Count.ShouldBe(3);

        var bloodFlow = result.Settings.First(s => s.Code.Contains("BLOOD_FLOW", StringComparison.OrdinalIgnoreCase));
        bloodFlow.ConstantValue.ShouldBe(300m);

        var ufRate = result.Settings.First(s => s.Code.Contains("UF_RATE", StringComparison.OrdinalIgnoreCase));
        ufRate.ConstantValue.ShouldBe(500m);

        var ufTarget = result.Settings.First(s => s.Code.Contains("UF_TARGET", StringComparison.OrdinalIgnoreCase));
        ufTarget.ConstantValue.ShouldBe(2000m);
    }

    [Fact]
    public void Parse_ProfiledSetting_ExtractsCorrectly()
    {
        const string withProfile = @"MSH|^~\&|EMR|FAC|MACH|FAC|20230215120000||RSP^K22^RSP_K21|MSG002|P|2.6
MSA|AA|MSG002
QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q002|@PID.3|MRN456^^^^MR
ORC|NW|ORD002^FAC
PID|||MRN456^^^^MR
OBX|1|NM|1.1.9.1.1^MDC_HDIALY_PROFILE_TYPE^MDC||LINEAR||ml/h|||||||RSET
OBX|2|NM|1.1.9.1.2^MDC_HDIALY_PROFILE_VALUE^MDC||250~500||ml/h|||||||RSET
OBX|3|NM|1.1.9.1.3^MDC_HDIALY_PROFILE_TIME^MDC||0~240||min|||||||RSET";

        var result = new RspK22Parser().Parse(withProfile);
        result.Settings.Count.ShouldBe(1);
        var setting = result.Settings[0];
        var profile = setting.Profile.ShouldNotBeNull();
        profile.Type.Value.ShouldBe("LINEAR");
        profile.Values.ShouldBe([250m, 500m]);
        profile.Times.ShouldBe([0m, 240m]);
    }

    [Fact]
    public void Parse_MissingMrn_Throws()
    {
        const string noMrn = @"MSH|^~\&|EMR|FAC|||20230215120000||RSP^K22^RSP_K21|MSG003|P|2.6
MSA|AA|MSG003
QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q003|@PID.3|^^^^MR
ORC|NW|ORD003";

        var ex = Should.Throw<ArgumentException>(() => new RspK22Parser().Parse(noMrn));
        ex.Message.ShouldContain("MRN");
    }
}
