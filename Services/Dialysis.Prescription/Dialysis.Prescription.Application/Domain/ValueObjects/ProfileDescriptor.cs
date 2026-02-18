namespace Dialysis.Prescription.Application.Domain.ValueObjects;

/// <summary>
/// Describes a profiled parameter: type plus control points for formula evaluation.
/// </summary>
public sealed record ProfileDescriptor(
    ProfileType Type,
    IReadOnlyList<decimal> Values,
    IReadOnlyList<decimal>? Times,
    decimal? HalfTimeMinutes,
    string? VendorName);
