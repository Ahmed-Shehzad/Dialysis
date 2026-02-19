using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Microsoft.EntityFrameworkCore;

using DeviceDomain = Dialysis.Device.Application.Domain.Device;

namespace Dialysis.Device.Infrastructure.Persistence;

public sealed class DeviceDbContext : DbContext, IDbContext
{
    public DeviceDbContext(DbContextOptions<DeviceDbContext> options)
        : base(options)
    {
    }

    public DbSet<DeviceDomain> Devices => Set<DeviceDomain>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<DeviceDomain>(e =>
        {
            _ = e.ToTable("Devices");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
            _ = e.Property(x => x.TenantId).HasMaxLength(100).HasDefaultValue(TenantContext.DefaultTenantId);
            _ = e.Property(x => x.DeviceEui64).HasMaxLength(200).IsRequired();
            _ = e.Property(x => x.Manufacturer).HasMaxLength(500);
            _ = e.Property(x => x.Model).HasMaxLength(200);
            _ = e.Property(x => x.Serial).HasMaxLength(200);
            _ = e.Property(x => x.Udi).HasMaxLength(500);
            _ = e.HasIndex(x => new { x.TenantId, x.DeviceEui64 }).IsUnique();
            _ = e.Ignore(x => x.DomainEvents);
            _ = e.Ignore(x => x.IntegrationEvents);
        });
    }
}
