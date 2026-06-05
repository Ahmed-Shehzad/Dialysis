using Dialysis.HIS.Integration.DeviceRegistry;

namespace Dialysis.HIS.Integration.Features.DeviceRegistry;

/// <summary>Compact device row for the registry list.</summary>
public sealed record DeviceSummaryDto(
    Guid Id,
    string DeviceId,
    string DeviceTypeCode,
    DeviceStatus Status,
    Guid? PatientId,
    DateTime? LastSeenAtUtc);

/// <summary>Full device projection (detail view).</summary>
public sealed record DeviceDto(
    Guid Id,
    string DeviceId,
    string DeviceTypeCode,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    DeviceStatus Status,
    Guid? PatientId,
    Guid? SessionId,
    DateTime? CalibrationDueUtc,
    DateTime RegisteredAtUtc,
    DateTime? LastSeenAtUtc);

internal static class DeviceProjections
{
    public static DeviceSummaryDto ToSummary(Device d) =>
        new(d.Id, d.DeviceId, d.DeviceTypeCode, d.Status, d.PatientId, d.LastSeenAtUtc);

    public static DeviceDto ToDto(Device d) =>
        new(d.Id, d.DeviceId, d.DeviceTypeCode, d.Manufacturer, d.Model, d.SerialNumber, d.Status,
            d.PatientId, d.SessionId, d.CalibrationDueUtc, d.RegisteredAtUtc, d.LastSeenAtUtc);
}
