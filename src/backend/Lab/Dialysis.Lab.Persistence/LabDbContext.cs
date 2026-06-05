using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Lab.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.Lab.Persistence;

/// <summary>
/// Laboratory bounded-context DbContext. Owns the lab order aggregate under the <c>lab_orders</c>
/// schema; inherits <see cref="ModuleDbContextBase"/> for the per-module schema convention and the
/// Transponder outbox/inbox/saga tables (under <c>transponder</c>).
/// </summary>
public sealed class LabDbContext : ModuleDbContextBase, IUnitOfWork
{
    public LabDbContext(
        DbContextOptions<LabDbContext> options,
        IOptions<TransponderPersistenceOptions> persistenceOptions)
        : base(options, persistenceOptions)
    {
    }

    protected override string ModuleSchema => "lab";

    public DbSet<LabOrder> LabOrders => Set<LabOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LabOrder>(e =>
        {
            e.ToTable("LabOrders", "lab_orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.PlacerOrderNumber).HasMaxLength(32).IsRequired();
            e.HasIndex(o => o.PlacerOrderNumber).IsUnique().HasDatabaseName("UX_LabOrders_PlacerOrderNumber");
            e.HasIndex(o => o.PatientId).HasDatabaseName("IX_LabOrders_PatientId");
            e.Property(o => o.FillerOrderNumber).HasMaxLength(64);
            e.Property(o => o.Specimen).HasMaxLength(256);
            e.Property(o => o.PlacedBy).HasMaxLength(128).IsRequired();
            e.Property(o => o.Priority).HasConversion<int>();
            e.Property(o => o.Status).HasConversion<int>();

            // Requested tests + returned observations ride inline as JSON so the order is one row and
            // the value-object lists can evolve without a join table. EF reads/writes the private fields.
            e.OwnsMany(o => o.Tests, t => t.ToJson());
            e.Navigation(o => o.Tests).HasField("_tests").UsePropertyAccessMode(PropertyAccessMode.Field);
            e.OwnsMany(o => o.Results, r => r.ToJson());
            e.Navigation(o => o.Results).HasField("_results").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
