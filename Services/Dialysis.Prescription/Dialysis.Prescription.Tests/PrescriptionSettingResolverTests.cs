using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Services;

using Shouldly;

namespace Dialysis.Prescription.Tests;

public sealed class PrescriptionSettingResolverTests
{
    [Fact]
    public void GetValueAtStart_ConstantSetting_ReturnsValue()
    {
        var settings = new List<ProfileSetting>
        {
            ProfileSetting.Constant("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING", 300)
        };
        PrescriptionSettingResolver.GetBloodFlowRateMlMin(settings).ShouldBe(300m);
    }

    [Fact]
    public void GetValueAtStart_MissingSetting_ReturnsNull()
    {
        var settings = new List<ProfileSetting>();
        PrescriptionSettingResolver.GetBloodFlowRateMlMin(settings).ShouldBeNull();
    }

    [Fact]
    public void GetValueAtStart_ProfiledSetting_EvaluatesAtZero()
    {
        var descriptor = new Application.Domain.ValueObjects.ProfileDescriptor(
            Application.Domain.ValueObjects.ProfileType.Constant,
            [250m],
            null,
            null,
            null);
        var settings = new List<ProfileSetting>
        {
            ProfileSetting.Profiled("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING", descriptor)
        };
        PrescriptionSettingResolver.GetBloodFlowRateMlMin(settings).ShouldBe(250m);
    }

    [Fact]
    public void GetUfRateMlH_ReturnsCorrectValue()
    {
        var settings = new List<ProfileSetting>
        {
            ProfileSetting.Constant(PrescriptionSettingResolver.Codes.UfRate, 500)
        };
        PrescriptionSettingResolver.GetUfRateMlH(settings).ShouldBe(500m);
    }

    [Fact]
    public void GetUfTargetVolumeMl_ReturnsCorrectValue()
    {
        var settings = new List<ProfileSetting>
        {
            ProfileSetting.Constant(PrescriptionSettingResolver.Codes.UfTargetVolume, 2000)
        };
        PrescriptionSettingResolver.GetUfTargetVolumeMl(settings).ShouldBe(2000m);
    }
}
