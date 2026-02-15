namespace Dialysis.Prediction.Services;

public sealed class VitalHistoryCache : IVitalHistoryCache
{
    private const int DefaultMaxPerPatient = 50;
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(30);

    private readonly Dictionary<string, LinkedList<VitalSnapshot>> _byPatient = new();
    private readonly object _lock = new();
    private readonly int _maxPerPatient;
    private readonly TimeSpan _maxAge;

    public VitalHistoryCache(int maxPerPatient = DefaultMaxPerPatient, TimeSpan? maxAge = null)
    {
        _maxPerPatient = maxPerPatient;
        _maxAge = maxAge ?? DefaultMaxAge;
    }

    public void Append(string patientId, VitalSnapshot vital)
    {
        if (string.IsNullOrEmpty(patientId)) return;
        lock (_lock)
        {
            if (!_byPatient.TryGetValue(patientId, out var list))
            {
                list = new LinkedList<VitalSnapshot>();
                _byPatient[patientId] = list;
            }
            list.AddLast(vital);
            while (list.Count > _maxPerPatient)
                list.RemoveFirst();
            PruneExpired(list, _maxAge);
        }
    }

    public IReadOnlyList<VitalSnapshot> GetRecent(string patientId, int maxCount = 20, TimeSpan? maxAge = null)
    {
        var age = maxAge ?? _maxAge;
        lock (_lock)
        {
            if (!_byPatient.TryGetValue(patientId, out var list))
                return [];
            PruneExpired(list, age);
            return list.TakeLast(maxCount).ToList();
        }
    }

    private void PruneExpired(LinkedList<VitalSnapshot> list, TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        while (list.First?.Value.Effective < cutoff)
            list.RemoveFirst();
    }
}
