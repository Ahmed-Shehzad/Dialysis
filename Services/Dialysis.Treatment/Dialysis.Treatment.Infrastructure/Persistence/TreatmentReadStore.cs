using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;
using Dialysis.Treatment.Infrastructure.Persistence;
using Dialysis.Treatment.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Treatment.Infrastructure;

public sealed class TreatmentReadStore : ITreatmentReadStore
{
    private readonly TreatmentReadDbContext _db;

    public TreatmentReadStore(TreatmentReadDbContext db)
    {
        _db = db;
    }

    public async Task<TreatmentSessionReadDto?> GetBySessionIdAsync(string tenantId, string sessionId, CancellationToken cancellationToken = default)
    {
        TreatmentSessionReadModel? session = await _db.TreatmentSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SessionId == sessionId, cancellationToken);
        if (session is null) return null;

        List<ObservationReadModel> obsList = await _db.Observations
            .AsNoTracking()
            .Where(o => o.TreatmentSessionId == session.Id)
            .OrderBy(o => o.ObservedAtUtc)
            .ToListAsync(cancellationToken);

        var observations = obsList.Select(ToObservationDto).ToList();
        PreAssessmentReadModel? preAssessment = await _db.PreAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SessionId == sessionId, cancellationToken);
        PreAssessmentDto? preAssessmentDto = preAssessment is not null ? ToPreAssessmentDto(preAssessment) : null;
        return new TreatmentSessionReadDto(
            session.SessionId,
            session.PatientMrn,
            session.DeviceId,
            session.DeviceEui64,
            session.TherapyId,
            session.Status,
            session.StartedAt,
            session.EndedAt,
            session.SignedAt,
            session.SignedBy,
            observations,
            preAssessmentDto);
    }

    public async Task<IReadOnlyList<TreatmentSessionReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default)
    {
        List<TreatmentSessionReadModel> sessions = await _db.TreatmentSessions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartedAt)
            .Take(Math.Max(1, Math.Min(limit, 1000)))
            .ToListAsync(cancellationToken);
        return await LoadSessionsWithObservationsAsync(sessions, cancellationToken);
    }

    public async Task<IReadOnlyList<TreatmentSessionReadDto>> SearchAsync(string tenantId, string? patientMrn, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, int limit, CancellationToken cancellationToken = default)
    {
        IQueryable<TreatmentSessionReadModel> query = _db.TreatmentSessions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(patientMrn))
            query = query.Where(s => s.PatientMrn == patientMrn);
        if (dateFrom.HasValue)
            query = query.Where(s => s.StartedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(s => s.StartedAt <= dateTo.Value);

        List<TreatmentSessionReadModel> sessions = await query
            .OrderByDescending(s => s.StartedAt)
            .Take(Math.Max(1, Math.Min(limit, 1000)))
            .ToListAsync(cancellationToken);
        return await LoadSessionsWithObservationsAsync(sessions, cancellationToken);
    }

    public async Task<IReadOnlyList<ObservationReadDto>> GetObservationsInTimeRangeAsync(string tenantId, string sessionId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken = default)
    {
        TreatmentSessionReadModel? session = await _db.TreatmentSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SessionId == sessionId, cancellationToken);
        if (session is null) return [];

        List<ObservationReadModel> obsList = await _db.Observations
            .AsNoTracking()
            .Where(o => o.TreatmentSessionId == session.Id && o.ObservedAtUtc >= startUtc && o.ObservedAtUtc <= endUtc)
            .OrderBy(o => o.ObservedAtUtc)
            .ToListAsync(cancellationToken);
        return obsList.Select(ToObservationReadDto).ToList();
    }

    private async Task<IReadOnlyList<TreatmentSessionReadDto>> LoadSessionsWithObservationsAsync(List<TreatmentSessionReadModel> sessions, CancellationToken cancellationToken)
    {
        if (sessions.Count == 0) return [];
        var sessionIds = sessions.Select(s => s.Id).ToList();
        var sessionIdStrings = sessions.Select(s => s.SessionId).Distinct().ToList();

        List<ObservationReadModel> allObs = await _db.Observations
            .AsNoTracking()
            .Where(o => sessionIds.Contains(o.TreatmentSessionId))
            .OrderBy(o => o.TreatmentSessionId)
            .ThenBy(o => o.ObservedAtUtc)
            .ToListAsync(cancellationToken);

        List<PreAssessmentReadModel> allPreAssessments = await _db.PreAssessments
            .AsNoTracking()
            .Where(p => p.TenantId == sessions[0].TenantId && sessionIdStrings.Contains(p.SessionId))
            .ToListAsync(cancellationToken);

        var obsBySession = allObs.GroupBy(o => o.TreatmentSessionId).ToDictionary(g => g.Key, g => g.ToList());
        var preAssessmentBySession = allPreAssessments.ToDictionary(p => p.SessionId, p => p);

        return sessions.Select(s =>
        {
            List<ObservationReadModel> obs = obsBySession.TryGetValue(s.Id, out List<ObservationReadModel>? list) ? list : [];
            var observationDtos = obs.Select(ToObservationDto).ToList();
            PreAssessmentDto? preAssessmentDto = preAssessmentBySession.TryGetValue(s.SessionId, out PreAssessmentReadModel? pa) && pa is not null
                ? ToPreAssessmentDto(pa)
                : null;
            return new TreatmentSessionReadDto(
                s.SessionId,
                s.PatientMrn,
                s.DeviceId,
                s.DeviceEui64,
                s.TherapyId,
                s.Status,
                s.StartedAt,
                s.EndedAt,
                s.SignedAt,
                s.SignedBy,
                observationDtos,
                preAssessmentDto);
        }).ToList();
    }

    private static PreAssessmentDto ToPreAssessmentDto(PreAssessmentReadModel p) =>
        new(p.PreWeightKg, p.BpSystolic, p.BpDiastolic, p.AccessTypeValue, p.PrescriptionConfirmed, p.PainSymptomNotes, p.RecordedAt, p.RecordedBy);

    private static ObservationDto ToObservationDto(ObservationReadModel o)
    {
        string? channelName = null;
        if (ContainmentPath.TryParse(o.SubId) is { } path && path.ChannelId is { } cid)
            channelName = ContainmentPath.GetChannelName(cid);
        return new ObservationDto(o.Code, o.Value, o.Unit, o.SubId, o.ReferenceRange, o.Provenance, o.EffectiveTime, channelName);
    }

    private static ObservationReadDto ToObservationReadDto(ObservationReadModel o)
    {
        string? channelName = null;
        if (ContainmentPath.TryParse(o.SubId) is { } path && path.ChannelId is { } cid)
            channelName = ContainmentPath.GetChannelName(cid);
        return new ObservationReadDto(o.Id, o.Code, o.Value, o.Unit, o.SubId, o.ObservedAtUtc, o.EffectiveTime, channelName);
    }
}
