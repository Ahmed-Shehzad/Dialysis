using Dialysis.Domain.Aggregates;
using Dialysis.Persistence;
using Dialysis.Persistence.Queries;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.SessionSummary;

/// <summary>
/// Builds a FHIR session summary bundle from a completed session. Enriches with
/// pre/post weight and BP from linked observations when available.
/// </summary>
public sealed class GetSessionSummaryQueryHandler : IQueryHandler<GetSessionSummaryQuery, GetSessionSummaryResult>
{
    private readonly DialysisDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly SessionSummaryPublisher _publisher;

    public GetSessionSummaryQueryHandler(DialysisDbContext db, ITenantContext tenantContext, SessionSummaryPublisher publisher)
    {
        _db = db;
        _tenantContext = tenantContext;
        _publisher = publisher;
    }

    public async Task<GetSessionSummaryResult> HandleAsync(GetSessionSummaryQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var session = await CompiledQueries.GetSessionById(_db, tenantId.Value, request.SessionId);

        if (session is null)
            return new GetSessionSummaryResult(null, null, "Session not found.");

        if (session.Status != Domain.Aggregates.SessionStatus.Completed)
            return new GetSessionSummaryResult(null, null, "Session must be completed to build summary.");

        if (!session.EndedAt.HasValue)
            return new GetSessionSummaryResult(null, null, "Session has no end time.");

        var input = await BuildInputAsync(session, cancellationToken);
        var bundle = _publisher.BuildBundle(input, request.BaseUrl);

        if (!string.IsNullOrEmpty(request.SaveToFilePath))
        {
            await _publisher.SaveToFileAsync(bundle, request.SaveToFilePath, cancellationToken);
        }

        var json = SessionSummaryPublisher.ToJson(bundle);
        return new GetSessionSummaryResult(bundle, json, null);
    }

    private async Task<SessionSummaryInput> BuildInputAsync(Session session, CancellationToken cancellationToken)
    {
        var windowStart = session.StartedAt.AddHours(-2);
        var windowEnd = session.EndedAt!.Value.AddHours(1);

        var observations = await _db.Observations
            .AsNoTracking()
            .Where(o => o.TenantId == session.TenantId && o.PatientId == session.PatientId &&
                        o.Effective.Value >= windowStart && o.Effective.Value <= windowEnd)
            .OrderBy(o => o.Effective.Value)
            .ToListAsync(cancellationToken);

        var weightObs = observations.Where(o => o.LoincCode.Value == LoincCode.BodyWeight.Value).ToList();
        var bpObs = observations.Where(o => o.LoincCode.Value == LoincCode.BloodPressure.Value).ToList();

        var preWeight = weightObs.LastOrDefault(o => o.Effective.Value <= session.StartedAt)?.NumericValue;
        var postWeight = weightObs.FirstOrDefault(o => o.Effective.Value >= session.EndedAt!.Value)?.NumericValue;
        var bp = bpObs.OrderByDescending(o => o.Effective.Value).FirstOrDefault();
        var systolicBp = bp?.NumericValue != null ? (int?)decimal.Round(bp.NumericValue.Value) : null;

        return new SessionSummaryInput(
            session.Id.ToString(),
            session.PatientId,
            session.TenantId,
            session.StartedAt,
            session.EndedAt!.Value,
            session.UfRemovedKg,
            session.AccessSite,
            preWeight,
            postWeight,
            systolicBp,
            null,
            null,
            true);
    }
}
