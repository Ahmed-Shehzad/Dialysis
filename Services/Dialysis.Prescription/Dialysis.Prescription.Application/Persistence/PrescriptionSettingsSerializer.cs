using System.Text.Json;

using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Domain.ValueObjects;

namespace Dialysis.Prescription.Application.Persistence;

/// <summary>
/// Serializes and deserializes prescription settings to/from JSON for persistence.
/// Keeps serialization logic in Application so EF never inspects nested value types.
/// </summary>
internal static class PrescriptionSettingsSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ToJson(IReadOnlyCollection<ProfileSetting> settings)
    {
        var dtos = settings.Select(ToDto).ToList();
        return JsonSerializer.Serialize(dtos, Options);
    }

    public static List<ProfileSetting> FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        List<SettingDto>? dtos = JsonSerializer.Deserialize<List<SettingDto>>(json, Options);
        if (dtos is null) return [];
        return dtos.Select(FromDto).ToList();
    }

    private static SettingDto ToDto(ProfileSetting s)
    {
        ProfileDescriptorDto? p = s.Profile is null ? null : new ProfileDescriptorDto(
            s.Profile.Type.Value,
            s.Profile.Values.ToList(),
            s.Profile.Times?.ToList(),
            s.Profile.HalfTimeMinutes,
            s.Profile.VendorName);
        return new SettingDto(s.Code, s.SubId, s.ConstantValue, p, s.Provenance);
    }

    private static ProfileSetting FromDto(SettingDto d)
    {
        if (d.ConstantValue.HasValue)
            return ProfileSetting.Constant(d.Code, d.ConstantValue.Value, d.SubId, d.Provenance);

        if (d.Profile is null)
            return ProfileSetting.Constant(d.Code, 0, d.SubId, d.Provenance);

        var descriptor = new ProfileDescriptor(
            new ProfileType(d.Profile.Type),
            d.Profile.Values,
            d.Profile.Times,
            d.Profile.HalfTimeMinutes,
            d.Profile.VendorName);
        return ProfileSetting.Profiled(d.Code, descriptor, d.SubId, d.Provenance);
    }

    private sealed record SettingDto(string Code, string? SubId, decimal? ConstantValue, ProfileDescriptorDto? Profile, string? Provenance);
    private sealed record ProfileDescriptorDto(string Type, List<decimal> Values, List<decimal>? Times, decimal? HalfTimeMinutes, string? VendorName);
}
