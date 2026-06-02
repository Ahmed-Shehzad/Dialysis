using Dialysis.PDMS.OnCall.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.PDMS.Persistence.Configurations;

/// <summary>
/// EF configuration for the escalation policy. One row per named policy; the dispatcher
/// reads the active policy on every alarm so we expect this table to stay tiny (one or
/// two rows total per facility) — no covering index needed.
/// </summary>
public sealed class EscalationPolicyConfiguration : IEntityTypeConfiguration<EscalationPolicy>
{
    public void Configure(EntityTypeBuilder<EscalationPolicy> b)
    {
        b.ToTable("EscalationPolicies", OnCallRotationConfiguration.SchemaName);
        b.HasKey(p => p.Id);
        b.Property(p => p.Name).IsRequired().HasMaxLength(128);
        b.Property(p => p.CriticalPrimaryWindow).IsRequired();
        b.Property(p => p.CriticalBackupWindow).IsRequired();
        b.Property(p => p.WarningPrimaryWindow).IsRequired();
        b.Property(p => p.WarningBackupWindow).IsRequired();
        b.Property(p => p.InformationalPrimaryWindow).IsRequired();
        b.Property(p => p.QuietHoursSuppressNonCritical).IsRequired();

        b.HasIndex(p => p.Name).IsUnique();

        b.Ignore(p => p.IntegrationEvents);
    }
}
