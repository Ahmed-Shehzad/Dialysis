using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Dialysis.DomainDrivenDesign.Persistence;

/// <summary>
/// Base <see cref="DbContext"/> for a modular monolith module. Stacks:
/// <list type="bullet">
///   <item>Transponder outbox/inbox/saga tables (via <see cref="TransponderPersistenceDbContextBase"/>).</item>
///   <item>Module-wide default schema (override <see cref="ModuleSchema"/>).</item>
///   <item><see cref="IUnitOfWork"/> implementation through <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.</item>
///   <item>The <see cref="AuditSaveChangesInterceptor"/> when one is resolvable from DI — wire it via <see cref="DbContextOptionsBuilder.AddInterceptors(Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[])"/> in the module's composition root.</item>
/// </list>
/// </summary>
public abstract class ModuleDbContextBase : TransponderPersistenceDbContextBase, IUnitOfWork
{
    /// <summary>
    /// Base <see cref="DbContext"/> for a modular monolith module. Stacks:
    /// <list type="bullet">
    ///   <item>Transponder outbox/inbox/saga tables (via <see cref="TransponderPersistenceDbContextBase"/>).</item>
    ///   <item>Module-wide default schema (override <see cref="ModuleSchema"/>).</item>
    ///   <item><see cref="IUnitOfWork"/> implementation through <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.</item>
    ///   <item>The <see cref="AuditSaveChangesInterceptor"/> when one is resolvable from DI — wire it via <see cref="DbContextOptionsBuilder.AddInterceptors(Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[])"/> in the module's composition root.</item>
    /// </list>
    /// </summary>
    protected ModuleDbContextBase(DbContextOptions options,
        IOptions<TransponderPersistenceOptions> persistenceOptions) : base(options, persistenceOptions)
    {
    }

    /// <summary>
    /// The default schema applied to every entity that does not explicitly set one.
    /// Override per module (e.g. <c>"his"</c>, <c>"ehr"</c>, <c>"pdms"</c>, <c>"identity"</c>, <c>"smartconnect"</c>).
    /// </summary>
    protected abstract string ModuleSchema { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchema);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Maps the <see cref="Audit"/> shadow columns onto a derived entity so the audit
    /// interceptor has stable column names across providers.
    /// </summary>
    public static void MapAuditShadow<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property(nameof(Audit.CreatedAt)).HasColumnName("CreatedAt");
        builder.Property(nameof(Audit.CreatedBy)).HasColumnName("CreatedBy").HasMaxLength(256);
        builder.Property(nameof(Audit.UpdatedAt)).HasColumnName("UpdatedAt");
        builder.Property(nameof(Audit.UpdatedBy)).HasColumnName("UpdatedBy").HasMaxLength(256);
        builder.Property(nameof(Audit.IsDeleted)).HasColumnName("IsDeleted");
        builder.Property(nameof(Audit.DeletedAt)).HasColumnName("DeletedAt");
        builder.Property(nameof(Audit.DeletedBy)).HasColumnName("DeletedBy").HasMaxLength(256);
    }
}
