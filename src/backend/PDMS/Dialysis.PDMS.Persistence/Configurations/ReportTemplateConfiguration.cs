using Dialysis.PDMS.Reporting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.PDMS.Persistence.Configurations;

/// <summary>
/// EF configuration for <see cref="ReportTemplate"/>. Versions live in a child table so
/// rollback (= republish an earlier version number) is a row-level operation rather than
/// a full-aggregate rewrite. Slug is the natural lookup operator-facing tools use.
/// </summary>
public sealed class ReportTemplateConfiguration : IEntityTypeConfiguration<ReportTemplate>
{
    public void Configure(EntityTypeBuilder<ReportTemplate> b)
    {
        b.ToTable("ReportTemplates", SessionReportConfiguration.SchemaName);
        b.HasKey(t => t.Id);
        b.Property(t => t.Slug).IsRequired().HasMaxLength(128);
        b.Property(t => t.Kind).HasConversion<int>().IsRequired();
        b.Property(t => t.Title).IsRequired().HasMaxLength(256);
        b.Property(t => t.PublishedVersionNumber);

        b.HasIndex(t => new { t.Slug, t.Kind }).IsUnique();

        b.OwnsMany(t => t.Versions, v =>
        {
            v.ToTable("ReportTemplateVersions", SessionReportConfiguration.SchemaName);
            v.WithOwner().HasForeignKey("TemplateId");
            v.HasKey("TemplateId", nameof(ReportTemplateVersion.VersionNumber));
            v.Property(x => x.VersionNumber);
            v.Property(x => x.BodyMarkdown).IsRequired();
            v.Property(x => x.AuthoredBySub).IsRequired().HasMaxLength(256);
            v.Property(x => x.AuthoredAtUtc).IsRequired();
        });

        b.Ignore(t => t.IntegrationEvents);
    }
}
