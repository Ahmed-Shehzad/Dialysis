using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Fhir.Infrastructure.Persistence;

public sealed class FhirDbContext : DbContext, IDbContext
{
    public FhirDbContext(DbContextOptions<FhirDbContext> options)
        : base(options)
    {
    }

    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<SubscriptionEntity>(e =>
        {
            _ = e.ToTable("Subscriptions");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id).HasMaxLength(50);
            _ = e.Property(x => x.TenantId).HasMaxLength(100).HasDefaultValue(TenantContext.DefaultTenantId);
            _ = e.Property(x => x.Status).HasMaxLength(20);
            _ = e.Property(x => x.ChannelType).HasMaxLength(30);
            _ = e.Property(x => x.Endpoint).HasMaxLength(2048);
            _ = e.Property(x => x.Criteria).HasMaxLength(500);
            _ = e.Property(x => x.ResourceJson).HasColumnType("jsonb");
            _ = e.HasIndex(x => new { x.TenantId, x.Status });
        });
    }
}
