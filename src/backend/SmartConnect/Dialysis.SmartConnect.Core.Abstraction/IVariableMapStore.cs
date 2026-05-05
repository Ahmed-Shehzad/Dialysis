namespace Dialysis.SmartConnect;

/// <summary>
/// Scoped key-value store accessible from channel scripts and transforms.
/// </summary>
public interface IVariableMapStore
{
    Task<string?> GetAsync(VariableMapScope scope, Guid? flowId, string key, CancellationToken cancellationToken = default);

    Task SetAsync(VariableMapScope scope, Guid? flowId, string key, string value, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetAllAsync(VariableMapScope scope, Guid? flowId, CancellationToken cancellationToken = default);

    Task RemoveAsync(VariableMapScope scope, Guid? flowId, string key, CancellationToken cancellationToken = default);
}

public enum VariableMapScope
{
    /// <summary>Scoped to a single message dispatch (not persisted).</summary>
    Channel = 0,

    /// <summary>Scoped to a flow's lifetime in memory (reset on flow redeploy).</summary>
    GlobalChannel = 1,

    /// <summary>Server-wide, persisted across restarts.</summary>
    Global = 2,

    /// <summary>Server-wide, read-only in scripts, managed via admin API.</summary>
    Configuration = 3,
}
