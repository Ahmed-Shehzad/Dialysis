using System.Collections.Concurrent;

namespace Dialysis.HIS.Integration.DeviceIngestion;

public sealed class SlidingWindowRateLimiter
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _queues = new();
    private readonly int _maxEventsPerWindow;
    private readonly TimeSpan _window;
    public SlidingWindowRateLimiter(int maxEventsPerWindow, TimeSpan window)
    {
        _maxEventsPerWindow = maxEventsPerWindow;
        _window = window;
    }

    public void ThrowIfExceeded(string key)
    {
        var q = _queues.GetOrAdd(key, static _ => new ConcurrentQueue<DateTime>());
        var cutoff = DateTime.UtcNow - _window;
        while (q.TryPeek(out var head) && head < cutoff)
            q.TryDequeue(out _);

        if (q.Count >= _maxEventsPerWindow)
            throw new InvalidOperationException($"Rate limit exceeded for '{key}'.");

        q.Enqueue(DateTime.UtcNow);
    }
}
