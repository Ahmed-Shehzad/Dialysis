namespace BuildingBlocks.Abstractions;

/// <summary>
/// Marker for read-only DbContexts. Do not implement IDbContext.
/// Read DbContexts should override SaveChanges to throw.
/// </summary>
public interface IReadOnlyDbContext
{
}
