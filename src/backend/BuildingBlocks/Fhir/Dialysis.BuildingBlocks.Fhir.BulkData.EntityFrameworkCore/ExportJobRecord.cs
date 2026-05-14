using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;

public sealed class ExportJobRecord
{
    public required string Id { get; set; }

    public required ExportScope Scope { get; set; }

    public string? GroupId { get; set; }

    public required string ResourceTypesCsv { get; set; }

    public DateTimeOffset? Since { get; set; }

    public string? DeIdentificationProfile { get; set; }

    public string? RequestorId { get; set; }

    public required ExportJobStatus Status { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? Error { get; set; }

    public required string OutputsJson { get; set; }
}

public sealed class ExportJobRecordConfiguration : IEntityTypeConfiguration<ExportJobRecord>
{
    public const string SchemaName = "fhir_export";

    public void Configure(EntityTypeBuilder<ExportJobRecord> builder)
    {
        builder.ToTable("export_jobs", SchemaName);
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasMaxLength(64);
        builder.Property(r => r.Scope).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.GroupId).HasMaxLength(128);
        builder.Property(r => r.ResourceTypesCsv).HasMaxLength(2048).IsRequired();
        builder.Property(r => r.DeIdentificationProfile).HasMaxLength(64);
        builder.Property(r => r.RequestorId).HasMaxLength(128);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.Error).HasMaxLength(2048);
        builder.Property(r => r.OutputsJson).IsRequired();
        builder.HasIndex(r => r.Status);
    }
}
