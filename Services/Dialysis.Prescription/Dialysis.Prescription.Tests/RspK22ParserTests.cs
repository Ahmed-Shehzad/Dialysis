using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Domain.ValueObjects;
using Dialysis.Prescription.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Prescription.Tests;

public sealed class RspK22ParserTests
{
    [Fact]
    public void Parse_ExtractsOrderId()
    {
        string orderId = PrescriptionTestData.OrderId();
        string mrn = PrescriptionTestData.Mrn();
        string rspK22 = PrescriptionTestData.MinimalRspK22(mrn, orderId, PrescriptionTestData.OrderingProvider(), PrescriptionTestData.CallbackPhone());
        RspK22ParseResult result = new RspK22Parser().Parse(rspK22);
        result.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void Parse_ExtractsPatientMrn()
    {
        string mrn = PrescriptionTestData.Mrn();
        string rspK22 = PrescriptionTestData.MinimalRspK22(mrn, PrescriptionTestData.OrderId(), PrescriptionTestData.OrderingProvider(), PrescriptionTestData.CallbackPhone());
        RspK22ParseResult result = new RspK22Parser().Parse(rspK22);
        result.PatientMrn.Value.ShouldBe(mrn);
    }

    [Fact]
    public void Parse_ExtractsMsaAndQueryInfo()
    {
        string rspK22 = PrescriptionTestData.MinimalRspK22(PrescriptionTestData.Mrn(), PrescriptionTestData.OrderId(), PrescriptionTestData.OrderingProvider(), PrescriptionTestData.CallbackPhone());
        RspK22ParseResult result = new RspK22Parser().Parse(rspK22);
        result.MsaAcknowledgmentCode.ShouldBe("AA");
        result.MsaControlId.ShouldBe("MSG001");
        result.QueryTag.ShouldBe("Q001");
        result.QpdQueryName.ShouldBe("MDC_HDIALY_RX_QUERY");
    }

    [Fact]
    public void Parse_ExtractsConstantSettings()
    {
        string rspK22 = PrescriptionTestData.MinimalRspK22(PrescriptionTestData.Mrn(), PrescriptionTestData.OrderId(), PrescriptionTestData.OrderingProvider(), PrescriptionTestData.CallbackPhone());
        RspK22ParseResult result = new RspK22Parser().Parse(rspK22);
        result.Settings.Count.ShouldBe(3);

        ProfileSetting bloodFlow = result.Settings.First(s => s.Code.Contains("BLOOD_FLOW", StringComparison.OrdinalIgnoreCase));
        bloodFlow.ConstantValue.ShouldBe(300m);

        ProfileSetting ufRate = result.Settings.First(s => s.Code.Contains("UF_RATE", StringComparison.OrdinalIgnoreCase));
        ufRate.ConstantValue.ShouldBe(500m);

        ProfileSetting ufTarget = result.Settings.First(s => s.Code.Contains("UF_TARGET", StringComparison.OrdinalIgnoreCase));
        ufTarget.ConstantValue.ShouldBe(2000m);
    }

    [Fact]
    public void Parse_ProfiledSetting_ExtractsCorrectly()
    {
        string mrn = PrescriptionTestData.Mrn();
        string orderId = PrescriptionTestData.OrderId();
        string withProfile = $"""
                             MSH|^~\&|EMR|FAC|MACH|FAC|20230215120000||RSP^K22^RSP_K21|MSG002|P|2.6
                             MSA|AA|MSG002
                             QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q002|@PID.3|{mrn}^^^^MR
                             ORC|NW|{orderId}^FAC
                             PID|||{mrn}^^^^MR
                             OBX|1|NM|1.1.9.1.1^MDC_HDIALY_PROFILE_TYPE^MDC||LINEAR||ml/h|||||||RSET
                             OBX|2|NM|1.1.9.1.2^MDC_HDIALY_PROFILE_VALUE^MDC||250~500||ml/h|||||||RSET
                             OBX|3|NM|1.1.9.1.3^MDC_HDIALY_PROFILE_TIME^MDC||0~240||min|||||||RSET
                             """;

        RspK22ParseResult result = new RspK22Parser().Parse(withProfile);
        result.Settings.Count.ShouldBe(1);
        ProfileSetting setting = result.Settings[0];
        ProfileDescriptor profile = setting.Profile.ShouldNotBeNull();
        profile.Type.Value.ShouldBe("LINEAR");
        profile.Values.ShouldBe([250m, 500m]);
        profile.Times.ShouldBe([0m, 240m]);
    }

    [Fact]
    public void Parse_MissingMrn_Throws()
    {
        const string noMrn = """
                             MSH|^~\&|EMR|FAC|||20230215120000||RSP^K22^RSP_K21|MSG003|P|2.6
                             MSA|AA|MSG003
                             QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q003|@PID.3|^^^^MR
                             ORC|NW|ORD003
                             """;

        ArgumentException ex = Should.Throw<ArgumentException>(() => new RspK22Parser().Parse(noMrn));
        ex.Message.ShouldContain("MRN");
    }
}
