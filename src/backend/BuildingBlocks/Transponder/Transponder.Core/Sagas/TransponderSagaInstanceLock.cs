using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>Serializes all saga work for the same (<paramref name="sagaKind"/>, <paramref name="instanceKey"/>) across message types.</summary>
internal static class TransponderSagaInstanceLock
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public static SemaphoreSlim Get(string sagaKind, string instanceKey) =>
        _locks.GetOrAdd(sagaKind + "\u001f" + instanceKey, _ => new SemaphoreSlim(1, 1));
}
