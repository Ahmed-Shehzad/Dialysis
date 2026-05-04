using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class SmartConnectDbContext(DbContextOptions<SmartConnectDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<IntegrationFlowEntity> IntegrationFlows => Set<IntegrationFlowEntity>();

    public DbSet<MessageLedgerEntryEntity> MessageLedgerEntries => Set<MessageLedgerEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationFlowEntity>(b =>
        {
            b.ToTable("IntegrationFlows", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(256).IsRequired();
            b.Property(e => e.PipelineJson).IsRequired();
        });

        modelBuilder.Entity<MessageLedgerEntryEntity>(b =>
        {
            b.ToTable("MessageLedgerEntries", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.CorrelationId).HasMaxLength(256).IsRequired();
            b.Property(e => e.Detail).HasMaxLength(4000);
            b.HasIndex(e => new { e.FlowId, e.CreatedAtUtc });
        });
    }
}
