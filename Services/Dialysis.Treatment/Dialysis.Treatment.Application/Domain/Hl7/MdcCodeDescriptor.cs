using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Hl7;

/// <summary>
/// Metadata for an IEEE 11073 MDC observation code: the expected containment level,
/// default UCUM unit, and human-readable display name.
/// </summary>
public sealed record MdcCodeDescriptor(
    ObservationCode Code,
    string DisplayName,
    ContainmentLevel Level,
    string? DefaultUnit,
    string? UcumUnit);
