using System.Collections.Concurrent;

namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// In-memory <see cref="IClockSkewMonitor"/> backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Each source's state is updated under a lock so the rolling max-window calculation is
/// observation-by-observation atomic. State is process-local — fine for the demo loop
/// where the SmartConnect host is single-replica; for a multi-replica deployment, swap
/// in an impl backed by the distributed cache.
/// </summary>
public sealed class InMemoryClockSkewMonitor : IClockSkewMonitor
{
    private readonly ConcurrentDictionary<string, SourceState> _state =
        new(StringComparer.OrdinalIgnoreCase);

    public void Record(ClockSkewObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var key = string.IsNullOrWhiteSpace(observation.SourceId) ? "(unknown)" : observation.SourceId.Trim();
        _state.AddOrUpdate(
            key,
            _ => new SourceState(observation),
            (_, existing) => existing.Combine(observation));
    }

    public IReadOnlyList<ClockSkewStatus> List()
    {
        return [.. _state
            .Select(kv => kv.Value.ToStatus(kv.Key))
            .OrderBy(s => s.SourceId, StringComparer.Ordinal)];
    }

    private sealed class SourceState
    {
        private readonly Lock _gate = new();
        private DateTime _lastObservedAtUtc;
        private TimeSpan _lastSkew;
        private TimeSpan _maxAbsSkew;
        private int _count;

        public SourceState(ClockSkewObservation initial)
        {
            _lastObservedAtUtc = initial.ObservedAtUtc;
            _lastSkew = initial.Skew;
            _maxAbsSkew = initial.Skew.Duration();
            _count = 1;
        }

        public SourceState Combine(ClockSkewObservation next)
        {
            lock (_gate)
            {
                _lastObservedAtUtc = next.ObservedAtUtc;
                _lastSkew = next.Skew;
                var abs = next.Skew.Duration();
                if (abs > _maxAbsSkew)
                    _maxAbsSkew = abs;
                _count += 1;
                return this;
            }
        }

        public ClockSkewStatus ToStatus(string sourceId)
        {
            lock (_gate)
            {
                return new ClockSkewStatus(
                    SourceId: sourceId,
                    LastObservedAtUtc: _lastObservedAtUtc,
                    LastSkew: _lastSkew,
                    MaxAbsSkewWindow: _maxAbsSkew,
                    ObservationCount: _count);
            }
        }
    }
}
