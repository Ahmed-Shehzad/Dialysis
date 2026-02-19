using BuildingBlocks.Abstractions;

using Dialysis.Device.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Device.Infrastructure.Persistence;

/// <summary>
/// Read-only DbContext for Device queries. Maps to the same database as DeviceDbContext.
/// SaveChanges throws; use only for reads.
/// </summary>
public sealed class DeviceReadDbContext : DbContext, IReadOnlyDbContext
{
    public DeviceReadDbContext(DbContextOptions<DeviceReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<DeviceReadModel> Devices => Set<DeviceReadModel>();

    public override int SaveChanges() =>
        throw new InvalidOperationException("DeviceReadDbContext is read-only. Do not call SaveChanges.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("DeviceReadDbContext is read-only. Do not call SaveChangesAsync.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<DeviceReadModel>(e =>
        {
            _ = e.ToTable("Devices");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id);
            _ = e.Property(x => x.TenantId).HasMaxLength(100);
            _ = e.Property(x => x.DeviceEui64).HasMaxLength(200);
            _ = e.Property(x => x.Manufacturer).HasMaxLength(500);
            _ = e.Property(x => x.Model).HasMaxLength(200);
            _ = e.Property(x => x.Serial).HasMaxLength(200);
            _ = e.Property(x => x.Udi).HasMaxLength(500);
            _ = e.HasIndex(x => new { x.TenantId, x.DeviceEui64 });
        });
    }
}
