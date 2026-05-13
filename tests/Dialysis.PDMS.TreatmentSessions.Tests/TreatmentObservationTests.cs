using Dialysis.PDMS.TreatmentSessions.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.TreatmentSessions.Tests;

public sealed class TreatmentObservationTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Record_numeric_observation_succeeds()
    {
        var observation = TreatmentObservation.Record(
            id: Guid.NewGuid(),
            sessionId: Guid.NewGuid(),
            machineId: Guid.NewGuid(),
            observedAtUtc: T0,
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
    public void Record_string_observation_succeeds()
    {
        var observation = TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0,
            mdcCode: 158594L, containmentPath: "1.1",
            valueNumeric: null, valueString: "TX",
            units: null, profileValues: null, profileTimesSeconds: null,
            sourceMessageId: Guid.NewGuid());

        observation.ValueString.ShouldBe("TX");
        observation.ValueNumeric.ShouldBeNull();
    }

    [Fact]
    public void Record_profile_observation_succeeds()
    {
        var observation = TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0,
            mdcCode: 12345L, containmentPath: "1.1.4.27",
            valueNumeric: null, valueString: null,
            units: "ml/h",
            profileValues: new[] { 600m, 800m, 400m },
            profileTimesSeconds: new[] { 0, 600, 1800 },
            sourceMessageId: Guid.NewGuid());

        observation.ProfileValues.ShouldBe(new[] { 600m, 800m, 400m });
        observation.ProfileTimesSeconds.ShouldBe(new[] { 0, 600, 1800 });
    }

    [Fact]
    public void Record_with_no_value_throws()
    {
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0,
            mdcCode: 1L, containmentPath: "1",
            valueNumeric: null, valueString: null,
            units: null, profileValues: null, profileTimesSeconds: null,
            sourceMessageId: Guid.NewGuid()));
    }

    [Fact]
    public void Record_with_two_values_throws()
    {
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0,
            mdcCode: 1L, containmentPath: "1",
            valueNumeric: 1.0m, valueString: "x",
            units: null, profileValues: null, profileTimesSeconds: null,
            sourceMessageId: Guid.NewGuid()));
    }

    [Fact]
    public void Record_profile_with_mismatched_time_array_throws()
    {
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0,
            mdcCode: 1L, containmentPath: "1",
            valueNumeric: null, valueString: null,
            units: null,
            profileValues: new[] { 1m, 2m, 3m },
            profileTimesSeconds: new[] { 0, 10 },
            sourceMessageId: Guid.NewGuid()));
    }

    [Fact]
    public void Record_rejects_invalid_inputs()
    {
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), T0,
            1L, "1", 1m, null, null, null, null, Guid.NewGuid()));
        Should.Throw<ArgumentException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, T0,
            1L, "1", 1m, null, null, null, null, Guid.NewGuid()));
        Should.Throw<ArgumentOutOfRangeException>(() => TreatmentObservation.Record(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0,
            0L, "1", 1m, null, null, null, null, Guid.NewGuid()));
    }
}
