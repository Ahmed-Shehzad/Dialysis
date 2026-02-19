using Microsoft.EntityFrameworkCore.ChangeTracking;

using Dialysis.Prescription.Application.Domain;

namespace Dialysis.Prescription.Infrastructure.Persistence;

internal static class ProfileSettingListValueComparer
{
    public static ValueComparer<List<ProfileSetting>> Instance { get; } = new(
        (a, b) => ListsEqual(a, b),
        c => ComputeHashCode(c));

    private static bool ListsEqual(List<ProfileSetting>? a, List<ProfileSetting>? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return PrescriptionSettingsSerializer.ToJson(a) == PrescriptionSettingsSerializer.ToJson(b);
    }

    private static int ComputeHashCode(List<ProfileSetting>? c)
    {
        return c == null ? 0 : PrescriptionSettingsSerializer.ToJson(c).GetHashCode();
    }
}
