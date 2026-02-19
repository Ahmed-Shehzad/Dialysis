using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain;
using Dialysis.Alarm.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Alarm.Tests;

public sealed class OruR40ParserTests
{
    private const string MinimalOruR40 = """
                                         MSH|^~\&|MACH_EUI64|FAC|EMR|FAC|20230215120000||ORU^R40^ORU_R40|MSG001|P|2.6
                                         PID|||MRN123^^^^MR
                                         OBR|1||THERAPY001^MACH^EUI64
                                         OBX|1|ST|MDC_EVT_HI_VAL_ALARM^12345^MDC|1.1.3.1.1|MDC_PRESS_BLD_ART^150020^MDC|mmHg
                                         OBX|2|NM|MDC_PRESS_BLD_ART^12345^MDC|1.1.3.1.2|180|mmHg|||H|||20230215120000
                                         OBX|3|ST|MDC_ATTR_EVT_PHASE^68481^MDC|1.1.3.1.3|start
                                         OBX|4|ST|MDC_ATTR_ALARM_STATE^68482^MDC|1.1.3.1.4|active
                                         OBX|5|ST|MDC_ATTR_ALARM_INACTIVATION_STATE^68483^MDC|1.1.3.1.5|enabled
                                         """;

    [Fact]
    public void Parse_ExtractsDeviceId()
    {
        OruR40ParseResult result = new OruR40Parser().Parse(MinimalOruR40);
        result.DeviceId.ShouldBe("MACH_EUI64");
    }

    [Fact]
    public void Parse_ExtractsSessionId()
    {
        OruR40ParseResult result = new OruR40Parser().Parse(MinimalOruR40);
        result.SessionId.ShouldBe("THERAPY001");
    }

    [Fact]
    public void Parse_ExtractsAlarm()
    {
        OruR40ParseResult result = new OruR40Parser().Parse(MinimalOruR40);
        result.Alarms.Count.ShouldBe(1);

        AlarmInfo alarm = result.Alarms[0];
        alarm.EventPhase.Value.ShouldBe("start");
        alarm.AlarmState.Value.ShouldBe("active");
        alarm.ActivityState.Value.ShouldBe("enabled");
    }

    [Fact]
    public void Parse_NullOrEmpty_Throws()
    {
        _ = Should.Throw<ArgumentException>(() => new OruR40Parser().Parse(""));
        _ = Should.Throw<ArgumentException>(() => new OruR40Parser().Parse(null!));
    }

    [Fact]
    public void Parse_Obx8InterpretationCodes_ExtractsPriorityTypeAndAbnormality()
    {
        const string withObx8 = """
                                MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||ORU^R40^ORU_R40|MSG003|P|2.6
                                PID|||MRN789^^^^MR
                                OBR|1||SESS003
                                OBX|1|ST|196648^MDC_EVT_HI^MDC|1.0.0.0.1|158776^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC|||PH~ST~H
                                OBX|2|NM|158776^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC|1.0.0.0.2|450|mm[Hg]|||20-400
                                OBX|3|ST|68481^MDC_ATTR_EVT_PHASE^MDC|1.0.0.0.3|start
                                OBX|4|ST|68482^MDC_ATTR_ALARM_STATE^MDC|1.0.0.0.4|active
                                OBX|5|ST|68483^MDC_ATTR_ALARM_INACTIVATION_STATE^MDC|1.0.0.0.5|enabled
                                """;

        OruR40ParseResult result = new OruR40Parser().Parse(withObx8);
        result.Alarms.Count.ShouldBe(1);

        AlarmInfo alarm = result.Alarms[0];
        alarm.Priority.HasValue.ShouldBeTrue();
        alarm.Priority!.Value.Value.ShouldBe("PH");
        alarm.InterpretationType.ShouldBe("ST");
        alarm.Abnormality.ShouldBe("H");
    }

    [Fact]
    public void Parse_NoAlarmGroups_ReturnsEmptyList()
    {
        const string noAlarms = """
                                MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||ORU^R40^ORU_R40|MSG002|P|2.6
                                PID|||MRN456^^^^MR
                                OBR|1||THERAPY002^MACH^EUI64
                                OBX|1|NM|12345^MDC_PRESS_BLD_ART^MDC||120|mmHg
                                """;

        OruR40ParseResult result = new OruR40Parser().Parse(noAlarms);
        result.Alarms.Count.ShouldBe(0);
    }
}
