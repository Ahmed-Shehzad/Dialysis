namespace BuildingBlocks.Abstractions;

public interface IDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
