using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Integration.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Integration.Features.Surveillance;

/// <summary>Count of adverse events for one (kind, severity) over the window.</summary>
public sealed record SurveillanceBucket(string Kind, string Severity, int Count);

/// <summary>
/// A flagged spike: the count of a kind in the current window is materially higher than the prior
/// equal-length baseline window.
/// </summary>
public sealed record SurveillanceSpike(string Kind, int CurrentCount, int BaselineCount);

/// <summary>Adverse-event surveillance snapshot over a window, with spike flags.</summary>
public sealed record SurveillanceResult(
    int WindowDays,
    int Total,
    IReadOnlyList<SurveillanceBucket> Buckets,
    IReadOnlyList<SurveillanceSpike> Spikes);

/// <summary>
/// Cross-patient adverse-event surveillance: counts by (kind, severity) over the window plus a
/// deterministic spike flag (current window vs the prior equal-length baseline).
/// </summary>
public sealed record GetAdverseEventSurveillanceQuery(int WindowDays = 7, int Take = 500)
    : IQuery<SurveillanceResult>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.SafetySurveillanceRead;
}

public sealed class GetAdverseEventSurveillanceQueryHandler
    : IQueryHandler<GetAdverseEventSurveillanceQuery, SurveillanceResult>
{
    // A kind spikes when the current window has at least this many events AND at least this multiple of
    // the prior baseline. Deterministic; no external analytics.
    private const int SpikeFloor = 3;
    private const double SpikeFactor = 2.0;

    private readonly IAdverseEventRepository _events;
    private readonly TimeProvider _timeProvider;

    public GetAdverseEventSurveillanceQueryHandler(IAdverseEventRepository events, TimeProvider timeProvider)
    {
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<SurveillanceResult> HandleAsync(
        GetAdverseEventSurveillanceQuery request, CancellationToken cancellationToken)
    {
        var windowDays = Math.Clamp(request.WindowDays, 1, 365);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = now.AddDays(-windowDays);
        var baselineStart = now.AddDays(-2 * windowDays);

        // Pull both windows in one read (baseline + current), most-recent first.
        var rows = await _events.ListSinceAsync(baselineStart, Math.Clamp(request.Take, 1, 5000), cancellationToken).ConfigureAwait(false);

        var current = rows.Where(r => r.OccurredAtUtc >= windowStart).ToList();
        var baseline = rows.Where(r => r.OccurredAtUtc < windowStart).ToList();

        var buckets = current
            .GroupBy(r => (r.Kind, r.Severity))
            .Select(g => new SurveillanceBucket(g.Key.Kind, g.Key.Severity, g.Count()))
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Kind, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var baselineByKind = baseline.GroupBy(r => r.Kind).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var spikes = current
            .GroupBy(r => r.Kind)
            .Select(g => (Kind: g.Key, Count: g.Count(), Baseline: baselineByKind.GetValueOrDefault(g.Key, 0)))
            .Where(x => x.Count >= SpikeFloor && x.Count >= Math.Max(1, x.Baseline) * SpikeFactor)
            .Select(x => new SurveillanceSpike(x.Kind, x.Count, x.Baseline))
            .OrderByDescending(s => s.CurrentCount)
            .ToList();

        return new SurveillanceResult(windowDays, current.Count, buckets, spikes);
    }
}
