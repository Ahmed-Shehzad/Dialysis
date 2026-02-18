namespace Transponder.Persistence.EntityFramework.Abstractions;

/// <summary>
/// Creates Entity Framework DbContext instances for persistence operations.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public interface IEntityFrameworkDbContextFactory<out TContext>
{
    /// <summary>
    /// Creates a new DbContext instance.
    /// </summary>
    TContext CreateDbContext();
}
