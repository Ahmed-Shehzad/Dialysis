using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class SmartConnectDbContext(DbContextOptions<SmartConnectDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<IntegrationFlowEntity> IntegrationFlows => Set<IntegrationFlowEntity>();

    public DbSet<MessageLedgerEntryEntity> MessageLedgerEntries => Set<MessageLedgerEntryEntity>();

    public DbSet<FlowGroupEntity> FlowGroups => Set<FlowGroupEntity>();

    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();

    public DbSet<VariableMapEntry> VariableMapEntries => Set<VariableMapEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationFlowEntity>(b =>
        {
            b.ToTable("IntegrationFlows", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(256).IsRequired();
            b.Property(e => e.PipelineJson).IsRequired();
            b.Property(e => e.TagsJson).HasMaxLength(2000);
            b.Property(e => e.Description).HasMaxLength(2000);
            b.HasIndex(e => e.GroupId);
        });

        modelBuilder.Entity<MessageLedgerEntryEntity>(b =>
        {
            b.ToTable("MessageLedgerEntries", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.CorrelationId).HasMaxLength(256).IsRequired();
            b.Property(e => e.Detail).HasMaxLength(4000);
            b.HasIndex(e => new { e.FlowId, e.CreatedAtUtc });
        });

        modelBuilder.Entity<FlowGroupEntity>(b =>
        {
            b.ToTable("FlowGroups", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(256).IsRequired();
            b.Property(e => e.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<AuditEventEntity>(b =>
        {
            b.ToTable("AuditEvents", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.Summary).HasMaxLength(4000).IsRequired();
            b.Property(e => e.UserId).HasMaxLength(256);
            b.HasIndex(e => new { e.Category, e.Timestamp });
            b.HasIndex(e => e.FlowId);
        });

        modelBuilder.Entity<VariableMapEntry>(b =>
        {
            b.ToTable("VariableMapEntries", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.Key).HasMaxLength(512).IsRequired();
            b.Property(e => e.Value).IsRequired();
            b.HasIndex(e => new { e.Scope, e.FlowId, e.Key }).IsUnique();
        });
    }
}
