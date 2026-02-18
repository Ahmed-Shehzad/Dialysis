using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Domain.Services;

namespace Dialysis.Prescription.Application.Services;

/// <summary>
/// Resolves prescription setting values for API responses (constant or profiled at t=0).
/// </summary>
public static class PrescriptionSettingResolver
{
    private const decimal DefaultTotalTreatmentMinutes = 240;

    /// <summary>
    /// MDC codes for common prescription settings (IEEE 11073).
    /// </summary>
    public static class Codes
    {
        public const string BloodFlowRate = "MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING";
        public const string UfRate = "MDC_HDIALY_UF_RATE_SETTING";
        public const string UfTargetVolume = "MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE";
    }

    /// <summary>
    /// Returns the value of a setting at t=0 (start of treatment), or null if not found.
    /// For profiled settings, evaluates the profile formula at time 0.
    /// </summary>
    public static decimal? GetValueAtStart(IEnumerable<ProfileSetting> settings, string code)
    {
        ProfileSetting? s = settings.FirstOrDefault(x =>
            string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));

        if (s is null) return null;

        if (s.ConstantValue.HasValue)
            return s.ConstantValue.Value;

        if (s.Profile is null) return null;

        return ProfileCalculator.Evaluate(s.Profile, 0, DefaultTotalTreatmentMinutes);
    }

    public static decimal? GetBloodFlowRateMlMin(IEnumerable<ProfileSetting> settings) =>
        GetValueAtStart(settings, Codes.BloodFlowRate);

    public static decimal? GetUfRateMlH(IEnumerable<ProfileSetting> settings) =>
        GetValueAtStart(settings, Codes.UfRate);

    public static decimal? GetUfTargetVolumeMl(IEnumerable<ProfileSetting> settings) =>
        GetValueAtStart(settings, Codes.UfTargetVolume);
}
