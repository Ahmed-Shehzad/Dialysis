using Dialysis.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class FailedHl7MessageConfiguration : IEntityTypeConfiguration<FailedHl7Message>
{
    public void Configure(EntityTypeBuilder<FailedHl7Message> builder)
    {
        builder.ToTable("failed_hl7_messages");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasConversion(ulid => ulid.ToString(), s => Ulid.Parse(s))
            .HasMaxLength(64);

        builder.Property(f => f.TenantId)
            .HasConversion(t => t.Value, v => new SharedKernel.ValueObjects.TenantId(v))
            .HasMaxLength(64);

        builder.Property(f => f.RawMessage).IsRequired();
        builder.Property(f => f.ErrorMessage).IsRequired().HasMaxLength(2000);
        builder.Property(f => f.MessageControlId).HasMaxLength(128);
        builder.Property(f => f.FailedAtUtc).HasColumnName("FailedAt");
        builder.Property(f => f.RetryCount);

        builder.HasIndex(f => new { f.TenantId, f.FailedAtUtc });
    }
}
