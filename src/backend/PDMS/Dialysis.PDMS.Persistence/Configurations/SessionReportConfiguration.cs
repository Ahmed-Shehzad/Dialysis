using Dialysis.PDMS.Reporting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.PDMS.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="SessionReport"/>. The aggregate holds only
/// metadata + the SHA-256 hash; the actual PDF bytes live in the blob store keyed by
/// <c>StorageRef</c>. That split keeps the row small and lets the audit gate verify
/// integrity by hash without pulling the body.
/// </summary>
public sealed class SessionReportConfiguration : IEntityTypeConfiguration<SessionReport>
{
    public const string SchemaName = "pdms_reporting";

    public void Configure(EntityTypeBuilder<SessionReport> b)
    {
        b.ToTable("SessionReports", SchemaName);
        b.HasKey(r => r.Id);
        b.Property(r => r.SessionId).IsRequired();
        b.Property(r => r.PatientId).IsRequired();
        b.Property(r => r.Kind).HasConversion<int>().IsRequired();
        b.Property(r => r.Status).HasConversion<int>().IsRequired();
        b.Property(r => r.Format).IsRequired().HasMaxLength(64);
        b.Property(r => r.ContentHash).HasMaxLength(128);
        b.Property(r => r.StorageRef).HasMaxLength(512);
        b.Property(r => r.GeneratedAtUtc);
        b.Property(r => r.DeliveredAtUtc);
        b.Property(r => r.FailureReason).HasMaxLength(1000);

        b.HasIndex(r => r.SessionId);
        b.HasIndex(r => new { r.PatientId, r.Kind });

        b.Ignore(r => r.IntegrationEvents);
    }
}
