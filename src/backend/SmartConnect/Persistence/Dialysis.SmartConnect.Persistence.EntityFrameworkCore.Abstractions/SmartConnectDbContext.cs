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

    public DbSet<CodeTemplateLibraryEntity> CodeTemplateLibraries => Set<CodeTemplateLibraryEntity>();

    public DbSet<CodeTemplateEntity> CodeTemplates => Set<CodeTemplateEntity>();

    public DbSet<AttachmentEntity> Attachments => Set<AttachmentEntity>();

    public DbSet<AlertRuleEntity> AlertRules => Set<AlertRuleEntity>();

    public DbSet<AlertEventEntity> AlertEvents => Set<AlertEventEntity>();

    public DbSet<CasBlobRefEntity> CasBlobRefs => Set<CasBlobRefEntity>();

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
            // MetadataJson is unbounded text — small dicts in practice (sender id, message type,
            // outbound parameters JSON) but no hard cap so flows that stuff large blobs into
            // metadata (e.g. base64-encoded attachment refs) don't silently truncate.
            b.Property(e => e.MetadataJson);
            // Slice C2: searchable derived columns. Bounded at 256 chars — HL7v2's longest
            // message types (e.g. "ORU^R40^ORU_R40") + NCPDP transaction codes + FHIR
            // resource names all fit comfortably; truncation here would silently corrupt
            // a filter so we cap at a length nobody can hit by accident.
            b.Property(e => e.MessageType).HasMaxLength(256);
            b.Property(e => e.SenderId).HasMaxLength(256);
            b.HasIndex(e => new { e.FlowId, e.CreatedAtUtc });
            b.HasIndex(e => e.MessageType);
            b.HasIndex(e => e.SenderId);
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

        modelBuilder.Entity<CodeTemplateLibraryEntity>(b =>
        {
            b.ToTable("CodeTemplateLibraries", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(256).IsRequired();
            b.Property(e => e.Description).HasMaxLength(2000);
            b.Property(e => e.LinkedFlowIdsJson).IsRequired();
        });

        modelBuilder.Entity<CodeTemplateEntity>(b =>
        {
            b.ToTable("CodeTemplates", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(256).IsRequired();
            b.Property(e => e.Code).IsRequired();
            b.Property(e => e.ContextsJson).IsRequired();
            b.HasIndex(e => e.LibraryId);
            b.HasOne<CodeTemplateLibraryEntity>()
                .WithMany()
                .HasForeignKey(e => e.LibraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttachmentEntity>(b =>
        {
            b.ToTable("Attachments", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.MimeType).HasMaxLength(256).IsRequired();
            b.HasIndex(e => e.MessageId);
            b.HasIndex(e => e.FlowId);
            b.HasIndex(e => e.CreatedUtc);
        });

        modelBuilder.Entity<AlertRuleEntity>(b =>
        {
            b.ToTable("AlertRules", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(256).IsRequired();
            b.Property(e => e.Description).HasMaxLength(2000);
            b.Property(e => e.EnabledFlowIdsJson).IsRequired();
            b.Property(e => e.ErrorPatternsJson).IsRequired();
            b.Property(e => e.ActionsJson).IsRequired();
            b.HasIndex(e => e.Enabled);
        });

        modelBuilder.Entity<AlertEventEntity>(b =>
        {
            b.ToTable("AlertEvents", "smartconnect");
            b.HasKey(e => e.Id);
            b.Property(e => e.CorrelationId).HasMaxLength(256);
            b.Property(e => e.ErrorDetail).HasMaxLength(2000);
            b.Property(e => e.ActionOutcomesJson).IsRequired();
            b.HasIndex(e => e.RuleId);
            b.HasIndex(e => e.FlowId);
            b.HasIndex(e => e.OccurredAtUtc);
        });

        modelBuilder.Entity<CasBlobRefEntity>(b =>
        {
            b.ToTable("CasBlobRefs", "smartconnect");
            b.HasKey(e => e.Id);
            // 64 hex chars for SHA-256 — fixed-width keeps the column index dense.
            b.Property(e => e.ContentHash).HasMaxLength(64).IsRequired();
            b.HasIndex(e => e.AttachmentId).IsUnique();
            b.HasIndex(e => e.ContentHash);
        });
    }
}
