using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Treatment.Tests;

public sealed class OruR01ParserTests
{
    private const string MinimalOruR01 = @"MSH|^~\&||MACH_EUI64|EMR|FAC|20230215120000||ORU^R01^ORU_R01|MSG001|P|2.6
PID|||MRN123^^^^MR
OBR|1||THERAPY001^MACH^EUI64|||20230215120000||||||start
OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|300|ml/min^ml/min^UCUM|||||F|||20230215120000|||AMEAS";

    [Fact]
    public void Parse_ExtractsSessionId()
    {
        OruParseResult result = new OruR01Parser().Parse(MinimalOruR01);
        result.SessionId.Value.ShouldBe("THERAPY001");
    }

    [Fact]
    public void Parse_ExtractsPatientMrn()
    {
        OruParseResult result = new OruR01Parser().Parse(MinimalOruR01);
        result.PatientMrn.HasValue.ShouldBeTrue();
        result.PatientMrn!.Value.Value.ShouldBe("MRN123");
    }

    [Fact]
    public void Parse_ExtractsDeviceId()
    {
        OruParseResult result = new OruR01Parser().Parse(MinimalOruR01);
        result.DeviceId.HasValue.ShouldBeTrue();
        result.DeviceId!.Value.Value.ShouldBe("MACH_EUI64");
    }

    [Fact]
    public void Parse_ExtractsObservation()
    {
        OruParseResult result = new OruR01Parser().Parse(MinimalOruR01);
        result.Observations.Count.ShouldBe(1);

        ObservationInfo obs = result.Observations[0];
        obs.Code.Value.ShouldBe("152348");
        obs.Value.ShouldBe("300");
        obs.Unit.ShouldBe("ml/min");
        obs.Provenance.ShouldBe("AMEAS");
    }

    [Fact]
    public void Parse_MultipleObservations_ExtractsAll()
    {
        const string multiObs = @"MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||ORU^R01^ORU_R01|MSG002|P|2.6
PID|||MRN456^^^^MR
OBR|1||THERAPY002^MACH^EUI64
OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|300|ml/min^ml/min^UCUM
OBX|2|NM|152636^MDC_HDIALY_UF_RATE^MDC|1.1.9.1|500|mL/h^mL/h^UCUM
OBX|3|NM|150020^MDC_PRESS_BLD_ART^MDC|1.1.3.2|120|mmHg^mmHg^UCUM";

        OruParseResult result = new OruR01Parser().Parse(multiObs);
        result.Observations.Count.ShouldBe(3);
    }

    [Fact]
    public void Parse_ContainmentLevel_DeterminedFromSubId()
    {
        OruParseResult result = new OruR01Parser().Parse(MinimalOruR01);
        ObservationInfo obs = result.Observations[0];
        obs.SubId.ShouldBe("1.1.3.1");
        _ = obs.Level.ShouldNotBeNull();
    }

    [Fact]
    public void Parse_NullOrEmpty_Throws()
    {
        _ = Should.Throw<ArgumentException>(() => new OruR01Parser().Parse(""));
        _ = Should.Throw<ArgumentException>(() => new OruR01Parser().Parse(null!));
    }
}
