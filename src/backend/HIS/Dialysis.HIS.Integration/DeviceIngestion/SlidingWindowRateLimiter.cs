using System.Collections.Concurrent;

namespace Dialysis.HIS.Integration.DeviceIngestion;

public sealed class SlidingWindowRateLimiter(int maxEventsPerWindow, TimeSpan window)
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _queues = new();

    public void ThrowIfExceeded(string key)
    {
        var q = _queues.GetOrAdd(key, static _ => new ConcurrentQueue<DateTime>());
        var cutoff = DateTime.UtcNow - window;
        while (q.TryPeek(out var head) && head < cutoff)
            q.TryDequeue(out _);

        if (q.Count >= maxEventsPerWindow)
            throw new InvalidOperationException($"Rate limit exceeded for '{key}'.");

        q.Enqueue(DateTime.UtcNow);
    }
}
