using Dialysis.Treatment.Application.Features.GetTreatmentSession;

namespace Dialysis.Treatment.Application.Abstractions;

/// <summary>
/// Read-only store for Treatment queries. Used by query handlers instead of the write repository.
/// </summary>
public interface ITreatmentReadStore
{
    Task<TreatmentSessionReadDto?> GetBySessionIdAsync(string tenantId, string sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TreatmentSessionReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TreatmentSessionReadDto>> SearchAsync(string tenantId, string? patientMrn, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ObservationReadDto>> GetObservationsInTimeRangeAsync(string tenantId, string sessionId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken = default);
}

public sealed record TreatmentSessionReadDto(
    string SessionId,
    string? PatientMrn,
    string? DeviceId,
    string? DeviceEui64,
    string? TherapyId,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<ObservationDto> Observations);

public sealed record ObservationReadDto(
    string Id,
    string Code,
    string? Value,
    string? Unit,
    string? SubId,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset? EffectiveTime,
    string? ChannelName);
