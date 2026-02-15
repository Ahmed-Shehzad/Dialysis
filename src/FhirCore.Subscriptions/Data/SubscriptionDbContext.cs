using Microsoft.EntityFrameworkCore;

namespace FhirCore.Subscriptions.Data;

public sealed class SubscriptionDbContext : DbContext
{
    public SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> options) : base(options)
    {
    }

    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Status);
        });
    }
}
