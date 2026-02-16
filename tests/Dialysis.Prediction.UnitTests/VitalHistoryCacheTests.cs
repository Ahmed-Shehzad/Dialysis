using Dialysis.Prediction.Services;
using Shouldly;
using Xunit;

namespace Dialysis.Prediction.UnitTests;

public sealed class VitalHistoryCacheTests
{
    [Fact]
    public void Append_and_GetRecent_returns_vitals()
    {
        var cache = new VitalHistoryCache(maxPerPatient: 10);
        var now = DateTimeOffset.UtcNow;

        cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 120, now));
        cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 115, now.AddMinutes(-1)));

        var recent = cache.GetRecent("p1", 5);
        recent.Count.ShouldBe(2);
        recent[0].Value.ShouldBe(120);
        recent[1].Value.ShouldBe(115);
    }

    [Fact]
    public void GetRecent_different_patient_returns_empty()
    {
        var cache = new VitalHistoryCache();
        cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 120, DateTimeOffset.UtcNow));

        cache.GetRecent("p2").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Append_empty_or_null_patient_id_ignored(string? patientId)
    {
        var cache = new VitalHistoryCache();
        cache.Append(patientId!, new VitalSnapshot(VitalCodes.SystolicBp, 120, DateTimeOffset.UtcNow));

        cache.GetRecent(patientId ?? "").ShouldBeEmpty();
    }

    [Fact]
    public void GetRecent_respects_maxCount()
    {
        var cache = new VitalHistoryCache(maxPerPatient: 20);
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 10; i++)
            cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 120 - i, now.AddMinutes(-i)));

        var recent = cache.GetRecent("p1", 3);
        recent.Count.ShouldBe(3);
        recent[^1].Value.ShouldBe(111);
    }

    [Fact]
    public void Append_enforces_maxPerPatient_evicts_oldest()
    {
        var cache = new VitalHistoryCache(maxPerPatient: 5);
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 10; i++)
            cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 100 + i, now.AddMinutes(-i)));

        var recent = cache.GetRecent("p1", 20);
        recent.Count.ShouldBe(5);
        recent.Select(v => v.Value).ShouldBe([105, 106, 107, 108, 109]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void GetRecent_respects_maxCount_param(int maxCount)
    {
        var cache = new VitalHistoryCache(maxPerPatient: 100);
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 50; i++)
            cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 120, now.AddMinutes(-i)));

        var recent = cache.GetRecent("p1", maxCount);
        recent.Count.ShouldBe(Math.Min(50, Math.Max(0, maxCount)));
    }

    [Fact]
    public void Multiple_patients_isolated()
    {
        var cache = new VitalHistoryCache();
        var now = DateTimeOffset.UtcNow;

        cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 120, now));
        cache.Append("p2", new VitalSnapshot(VitalCodes.SystolicBp, 90, now));

        cache.GetRecent("p1").ShouldHaveSingleItem().Value.ShouldBe(120);
        cache.GetRecent("p2").ShouldHaveSingleItem().Value.ShouldBe(90);
    }

    [Fact]
    public void PruneExpired_removes_old_vitals()
    {
        var cache = new VitalHistoryCache(maxPerPatient: 50, maxAge: TimeSpan.FromMinutes(5));
        var now = DateTimeOffset.UtcNow;

        cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 115, now.AddMinutes(-10)));
        cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 120, now));

        var recent = cache.GetRecent("p1", 20);
        recent.ShouldHaveSingleItem().Value.ShouldBe(120);
    }

    [Fact]
    public void GetRecent_with_custom_maxAge_filters_by_specified_age()
    {
        var cache = new VitalHistoryCache(maxPerPatient: 50, maxAge: TimeSpan.FromMinutes(30));
        var now = DateTimeOffset.UtcNow;

        cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 115, now.AddMinutes(-5)));
        cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 120, now));

        var recent = cache.GetRecent("p1", 20, TimeSpan.FromMinutes(1));
        recent.ShouldHaveSingleItem().Value.ShouldBe(120);
    }

    [Fact]
    public void Append_various_vital_codes()
    {
        var cache = new VitalHistoryCache();
        var now = DateTimeOffset.UtcNow;

        cache.Append("p1", new VitalSnapshot(VitalCodes.SystolicBp, 120, now));
        cache.Append("p1", new VitalSnapshot(VitalCodes.HeartRate, 72, now));
        cache.Append("p1", new VitalSnapshot(VitalCodes.SpO2, 98, now));

        cache.GetRecent("p1", 10).Count.ShouldBe(3);
    }
}
