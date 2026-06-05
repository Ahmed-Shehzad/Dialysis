namespace Dialysis.BuildingBlocks.DistributedCache.Valkey;

/// <summary>
/// Binding target for <c>&lt;Module&gt;:DistributedCache:Valkey</c> in <c>appsettings.json</c>.
/// </summary>
public sealed class ValkeyDistributedCacheOptions
{
    /// <summary>
    /// Valkey connection string in the StackExchange.Redis format the GLIDE client accepts, e.g.
    /// <c>valkey:6379,abortConnect=false</c> for in-cluster access or a managed-service endpoint
    /// (AWS MemoryDB / ElastiCache, Azure Cache for Redis, Aiven Valkey, etc.). Required.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>Per-module key prefix so multiple modules sharing one Valkey cluster don't collide.</summary>
    public required string InstanceName { get; set; }

    /// <summary>
    /// When true, also stores the ASP.NET Core Data Protection key ring in Valkey under
    /// <c>{InstanceName}:data-protection-keys</c>. Required for horizontal scaling of any module that
    /// signs/encrypts cookies or anti-forgery tokens.
    /// </summary>
    public bool UseDataProtectionKeyRing { get; set; } = true;
}
