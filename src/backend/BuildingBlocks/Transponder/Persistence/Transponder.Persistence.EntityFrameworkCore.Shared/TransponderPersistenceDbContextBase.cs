using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Shared EF model; concrete <see cref="DbContext"/> types live in provider assemblies so migrations stay provider-specific.
/// </summary>
public abstract class TransponderPersistenceDbContextBase(
    DbContextOptions options,
    IOptions<TransponderPersistenceOptions> persistenceOptions)
    : DbContext(options)
{
    private readonly IOptions<TransponderPersistenceOptions> _persistenceOptions = persistenceOptions;

    public DbSet<TransponderOutboxMessageEntity> OutboxMessages => Set<TransponderOutboxMessageEntity>();

    public DbSet<TransponderInboxMessageEntity> InboxMessages => Set<TransponderInboxMessageEntity>();

    public DbSet<TransponderSagaInstanceEntity> SagaInstances => Set<TransponderSagaInstanceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _persistenceOptions.Value.Schema;
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        TransponderPersistenceModelConfiguration.Configure(modelBuilder, schema);
    }
}
