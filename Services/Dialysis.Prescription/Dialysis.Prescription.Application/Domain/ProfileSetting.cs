using Dialysis.Prescription.Application.Domain.ValueObjects;

namespace Dialysis.Prescription.Application.Domain;

/// <summary>
/// A single prescription setting from RSP^K22 OBX hierarchy.
/// May be constant or profiled (CONSTANT, LINEAR, EXPONENTIAL, STEP, VENDOR).
/// </summary>
public sealed class ProfileSetting
{
    public string Code { get; private set; } = string.Empty;
    public string? SubId { get; private set; }
    public decimal? ConstantValue { get; private set; }
    public ProfileDescriptor? Profile { get; private set; }
    public string? Provenance { get; private set; }

    private ProfileSetting() { }

    public static ProfileSetting Constant(string code, decimal value, string? subId = null, string? provenance = null)
    {
        return new ProfileSetting
        {
            Code = code,
            SubId = subId,
            ConstantValue = value,
            Provenance = provenance
        };
    }

    public static ProfileSetting Profiled(string code, ProfileDescriptor profile, string? subId = null, string? provenance = null)
    {
        return new ProfileSetting
        {
            Code = code,
            SubId = subId,
            Profile = profile,
            Provenance = provenance
        };
    }
}
