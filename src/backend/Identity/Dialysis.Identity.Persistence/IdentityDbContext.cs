using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Provisioning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.Identity.Persistence;

public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    IOptions<TransponderPersistenceOptions> persistenceOptions)
    : ModuleDbContextBase(options, persistenceOptions)
{
    private const string ProvisioningSchema = "identity_provisioning";

    protected override string ModuleSchema => "identity";

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<RoleDefinition> Roles => Set<RoleDefinition>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>(b =>
        {
            b.ToTable("UserAccounts", ProvisioningSchema);
            b.HasKey(u => u.Id);
            b.Property(u => u.Subject).HasMaxLength(256).IsRequired();
            b.HasIndex(u => u.Subject).IsUnique();
            b.Property(u => u.DisplayName).HasMaxLength(256).IsRequired();
            b.Property(u => u.Email).HasMaxLength(320);
            b.Property(u => u.Status).HasConversion<int>().IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<RoleDefinition>(b =>
        {
            b.ToTable("RoleDefinitions", ProvisioningSchema);
            b.HasKey(r => r.Id);
            b.Property(r => r.Code).HasMaxLength(128).IsRequired();
            b.HasIndex(r => r.Code).IsUnique();
            b.Property(r => r.DisplayName).HasMaxLength(256).IsRequired();
            b.Property(r => r.Permissions)
                .HasColumnType("text[]")
                .IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<RoleAssignment>(b =>
        {
            b.ToTable("RoleAssignments", ProvisioningSchema);
            b.HasKey(a => a.Id);
            b.HasIndex(a => new { a.UserId, a.RoleId }).IsUnique();
            b.Property(a => a.AssignedAtUtc).IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });
    }
}
