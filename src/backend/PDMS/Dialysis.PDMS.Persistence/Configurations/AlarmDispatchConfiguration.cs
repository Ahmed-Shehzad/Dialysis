using Dialysis.PDMS.OnCall.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.PDMS.Persistence.Configurations;

/// <summary>
/// EF configuration for the per-alarm dispatch audit aggregate. One row per alarm raised
/// against an infusion; the attempt history is a child table so the audit page can render
/// each delivery attempt + outcome without loading every alarm into memory.
/// </summary>
public sealed class AlarmDispatchConfiguration : IEntityTypeConfiguration<AlarmDispatch>
{
    public void Configure(EntityTypeBuilder<AlarmDispatch> b)
    {
        b.ToTable("AlarmDispatches", OnCallRotationConfiguration.SchemaName);
        b.HasKey(d => d.Id);
        b.Property(d => d.InfusionId).IsRequired();
        b.Property(d => d.SessionId).IsRequired();
        b.Property(d => d.ChairId).IsRequired();
        b.Property(d => d.AlarmCode).IsRequired().HasMaxLength(64);
        b.Property(d => d.Severity).HasConversion<int>().IsRequired();
        b.Property(d => d.Status).HasConversion<int>().IsRequired();
        b.Property(d => d.StartedAtUtc).IsRequired();
        b.Property(d => d.ResolvedAtUtc);
        b.Property(d => d.RotationId).IsRequired();
        b.Property(d => d.PolicyId).IsRequired();
        b.Property(d => d.CurrentLinkIndex).IsRequired();
        b.Property(d => d.AcknowledgedBySub).HasMaxLength(256);

        b.HasIndex(d => d.SessionId);
        b.HasIndex(d => new { d.ChairId, d.StartedAtUtc });
        b.HasIndex(d => d.Status);

        b.OwnsMany(d => d.Attempts, a =>
        {
            a.ToTable("AlarmDispatchAttempts", OnCallRotationConfiguration.SchemaName);
            a.WithOwner().HasForeignKey("AlarmDispatchId");
            a.Property<int>("Sequence").ValueGeneratedOnAdd();
            a.HasKey("AlarmDispatchId", "Sequence");
            a.Property(x => x.ChainLinkIndex).IsRequired();
            a.Property(x => x.Channel).HasConversion<int>().IsRequired();
            a.Property(x => x.Address).IsRequired().HasMaxLength(256);
            a.Property(x => x.Delivered).IsRequired();
            a.Property(x => x.FailureReason).HasMaxLength(1000);
            a.Property(x => x.AttemptedAtUtc).IsRequired();
        });

        b.Ignore(d => d.IntegrationEvents);
    }
}
