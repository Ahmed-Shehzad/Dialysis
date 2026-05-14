using Dialysis.PDMS.TreatmentSessions.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests;

public sealed class TreatmentObservationTests
{
    private static readonly DateTime _T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Record_Numeric_Observation_Succeeds()
    {
        var observation = TreatmentObservation.Record(
            id: Guid.NewGuid(),
            sessionId: Guid.NewGuid(),
            machineId: Guid.NewGuid(),
            observedAtUtc: _T0,
            mdcCode: 158612L,
            containmentPath: "1.1.4.27.1",
            valueNumeric: 145.5m,
            valueString: null,
            units: "mm[Hg]",
            profileValues: null,
            profileTimesSeconds: null,
            sourceMessageId: Guid.NewGuid());

        observation.MdcCode.ShouldBe(158612L);
        observation.ContainmentPath.ShouldBe("1.1.4.27.1");
        observation.ValueNumeric.ShouldBe(145.5m);
        observation.ValueString.ShouldBeNull();
        observation.ProfileValues.ShouldBeNull();
        observation.Units.ShouldBe("mm[Hg]");
    }

    [Fact]
    public void Record_String_Observation_Succeeds()
    {
        var observation = TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _T0,
            mdcCode: 158594L, containmentPath: "1.1",
            valueNumeric: null, valueString: "TX",
            units: null, profileValues: null, profileTimesSeconds: null,
            sourceMessageId: Guid.NewGuid());

        observation.ValueString.ShouldBe("TX");
        observation.ValueNumeric.ShouldBeNull();
    }

    [Fact]
    public void Record_Profile_Observation_Succeeds()
    {
        var observation = TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _T0,
            mdcCode: 12345L, containmentPath: "1.1.4.27",
            valueNumeric: null, valueString: null,
            units: "ml/h",
            profileValues: [600m, 800m, 400m],
            profileTimesSeconds: [0, 600, 1800],
            sourceMessageId: Guid.NewGuid());

        observation.ProfileValues.ShouldBe([600m, 800m, 400m]);
        observation.ProfileTimesSeconds.ShouldBe([0, 600, 1800]);
    }

    [Fact]
    public void Record_With_No_Value_Throws()
    {
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _T0,
            mdcCode: 1L, containmentPath: "1",
            valueNumeric: null, valueString: null,
            units: null, profileValues: null, profileTimesSeconds: null,
            sourceMessageId: Guid.NewGuid()));
    }

    [Fact]
    public void Record_With_Two_Values_Throws()
    {
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _T0,
            mdcCode: 1L, containmentPath: "1",
            valueNumeric: 1.0m, valueString: "x",
            units: null, profileValues: null, profileTimesSeconds: null,
            sourceMessageId: Guid.NewGuid()));
    }

    [Fact]
    public void Record_Profile_With_Mismatched_Time_Array_Throws()
    {
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _T0,
            mdcCode: 1L, containmentPath: "1",
            valueNumeric: null, valueString: null,
            units: null,
            profileValues: [1m, 2m, 3m],
            profileTimesSeconds: [0, 10],
            sourceMessageId: Guid.NewGuid()));
    }

    [Fact]
    public void Record_Rejects_Invalid_Inputs()
    {
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), _T0,
            1L, "1", 1m, null, null, null, null, Guid.NewGuid()));
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, _T0,
            1L, "1", 1m, null, null, null, null, Guid.NewGuid()));
        Should.Throw<ArgumentOutOfRangeException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _T0,
            0L, "1", 1m, null, null, null, null, Guid.NewGuid()));
    }
}
