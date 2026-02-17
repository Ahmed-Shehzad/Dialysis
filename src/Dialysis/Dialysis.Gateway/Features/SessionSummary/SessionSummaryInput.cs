using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Gateway.Features.SessionSummary;

/// <summary>
/// Input for building a dialysis session summary bundle. Can be created from a completed
/// Session entity or from a JSON mock (e.g. for testing or standalone use).
/// </summary>
public sealed record SessionSummaryInput(
    string SessionId,
    PatientId PatientId,
    TenantId TenantId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    decimal? UfRemovedKg = null,
    string? AccessSite = null,
    decimal? PreWeightKg = null,
    decimal? PostWeightKg = null,
    int? SystolicBp = null,
    int? DiastolicBp = null,
    string? Complications = null,
    bool IncludeProcedure = true
)
{
    /// <summary>
    /// Treatment duration in minutes.
    /// </summary>
    public int TreatmentMinutes => (int)(EndedAt - StartedAt).TotalMinutes;
}
