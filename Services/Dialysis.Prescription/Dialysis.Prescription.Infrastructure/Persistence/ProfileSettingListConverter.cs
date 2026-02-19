using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Dialysis.Prescription.Application.Domain;

namespace Dialysis.Prescription.Infrastructure.Persistence;

internal sealed class ProfileSettingListConverter : ValueConverter<List<ProfileSetting>, string>
{
    public static readonly ProfileSettingListConverter Instance = new();

    private ProfileSettingListConverter()
        : base(
            v => PrescriptionSettingsSerializer.ToJson(v),
            v => PrescriptionSettingsSerializer.FromJson(v))
    {
    }
}
