using Microsoft.EntityFrameworkCore;

namespace Transponder.Persistence.EntityFramework.Tests;

internal sealed class EntityFrameworkTestDbContext : TransponderDbContext
{
    public EntityFrameworkTestDbContext(DbContextOptions<EntityFrameworkTestDbContext> options)
        : base(options, new EntityFrameworkStorageOptions())
    {
    }
}
