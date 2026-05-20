namespace Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;

/// <summary>
/// Wire shape for one row of the receptionist's "Today" queue. The string status lets the
/// SPA stay decoupled from the C# enum; values are lower-kebab so the front-end union type
/// (<c>"expected" | "waiting" | "in-treatment"</c>) is the source of truth.
/// </summary>
public sealed record PatientQueueEntryDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string Mrn,
    DateTime ScheduledForUtc,
    string Status,
    string? Chair,
    bool EligibilityVerified);
