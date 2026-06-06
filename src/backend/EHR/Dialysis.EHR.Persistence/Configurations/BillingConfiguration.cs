using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.EHR.Persistence.Configurations;

internal static class BillingConfiguration
{
    private const string Schema = "ehr_billing";

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BillableEncounter>(b =>
        {
            b.ToTable("BillableEncounters", Schema);
            b.HasKey(e => e.EncounterId);
            b.Property(e => e.PatientId).IsRequired();
            b.Property(e => e.ProviderId).IsRequired();
            b.Property(e => e.ClosedAtUtc).IsRequired();
            b.Property(e => e.HasCharge).IsRequired();
            b.HasIndex(e => new { e.HasCharge, e.ClosedAtUtc });
        });

        modelBuilder.Entity<Payer>(b =>
        {
            b.ToTable("Payers", Schema);
            b.HasKey(p => p.Id);
            b.Property(p => p.PayerCode).HasMaxLength(32).IsRequired();
            b.HasIndex(p => p.PayerCode).IsUnique();
            b.Property(p => p.DisplayName).HasMaxLength(256).IsRequired();
            b.Property(p => p.ClearinghouseCode).HasMaxLength(64);
            b.Property(p => p.IsActive).IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<Charge>(b =>
        {
            b.ToTable("Charges", Schema);
            b.HasKey(c => c.Id);
            b.Property(c => c.PatientId).IsRequired();
            b.Property(c => c.EncounterId).IsRequired();
            b.HasIndex(c => c.PatientId);
            b.HasIndex(c => c.EncounterId);
            b.Property(c => c.CptCode).HasMaxLength(16).IsRequired();
            b.Property(c => c.Status).HasConversion<int>().IsRequired();
            b.Property<List<string>>("_diagnosisPointers")
                .HasColumnName("DiagnosisPointerIcd10Codes")
                .HasConversion(
                    v => string.Join(',', v ?? new List<string>()),
                    v => v.Length == 0 ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            b.OwnsOne(c => c.BilledAmount, MapMoney);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<Claim>(b =>
        {
            b.ToTable("Claims", Schema);
            b.HasKey(c => c.Id);
            b.Property(c => c.PatientId).IsRequired();
            b.Property(c => c.PayerId).IsRequired();
            b.HasIndex(c => c.PatientId);
            b.HasIndex(c => c.ExternalControlNumber).IsUnique().HasFilter("\"ExternalControlNumber\" IS NOT NULL");
            b.Property(c => c.PayerCode).HasMaxLength(32).IsRequired();
            b.Property(c => c.ClaimFormatCode).HasMaxLength(32).IsRequired();
            b.Property(c => c.ExternalControlNumber).HasMaxLength(64);
            b.Property(c => c.Status).HasConversion<int>().IsRequired();
            b.OwnsOne(c => c.BilledTotal, MapMoney);
            b.Property<List<Guid>>("_chargeIds")
                .HasColumnName("ChargeIds")
                .HasConversion(
                    v => string.Join(',', (v ?? new List<Guid>()).Select(g => g.ToString())),
                    v => v.Length == 0 ? new List<Guid>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList());
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<Remittance>(b =>
        {
            b.ToTable("Remittances", Schema);
            b.HasKey(r => r.Id);
            b.Property(r => r.ClaimId).IsRequired();
            b.HasIndex(r => r.ClaimId);
            b.Property(r => r.PayerCode).HasMaxLength(32).IsRequired();
            b.Property(r => r.AdjudicationStatus).HasConversion<int>().IsRequired();
            b.OwnsOne(r => r.PaidAmount, m =>
            {
                m.Property(x => x.Amount).HasColumnName("PaidAmount").HasPrecision(18, 2);
                m.Property(x => x.CurrencyCode).HasColumnName("PaidCurrency").HasMaxLength(3);
            });
            b.OwnsOne(r => r.AdjustmentAmount, m =>
            {
                m.Property(x => x.Amount).HasColumnName("AdjustmentAmount").HasPrecision(18, 2);
                m.Property(x => x.CurrencyCode).HasColumnName("AdjustmentCurrency").HasMaxLength(3);
            });
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<Payment>(b =>
        {
            b.ToTable("Payments", Schema);
            b.HasKey(p => p.Id);
            b.Property(p => p.PatientId).IsRequired();
            b.HasIndex(p => p.PatientId);
            b.Property(p => p.Method).HasConversion<int>().IsRequired();
            b.Property(p => p.ExternalReference).HasMaxLength(128);
            b.OwnsOne(p => p.Amount, MapMoney);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        // PR 7 — per-payer / per-CPT fee schedule + idempotency markers backing
        // EfCptFeeSchedule / EfChargeIdempotencyStore.
        modelBuilder.Entity<CptFeeScheduleEntry>(b =>
        {
            b.ToTable("CptFeeSchedule", Schema);
            b.HasKey(e => e.Id);
            b.Property(e => e.CptCode).IsRequired().HasMaxLength(8);
            b.Property(e => e.PayerCode).IsRequired().HasMaxLength(32);
            b.Property(e => e.EffectiveFromUtc).IsRequired();
            b.Property(e => e.EffectiveUntilUtc);
            b.OwnsOne(e => e.Amount, MapMoney);
            // Covering index for the EfCptFeeSchedule resolution query.
            b.HasIndex(e => new { e.CptCode, e.PayerCode, e.EffectiveFromUtc });
        });

        modelBuilder.Entity<ChargeIdempotencyMarker>(b =>
        {
            b.ToTable("ChargeIdempotencyMarkers", Schema);
            // Composite key + unique index gives the database-level guarantee against
            // concurrent re-delivery from two replicas at once.
            b.HasKey(m => new { m.SessionId, m.CptCode });
            b.Property(m => m.CptCode).HasMaxLength(8);
            b.Property(m => m.ChargeId).IsRequired();
            b.Property(m => m.CapturedAtUtc).IsRequired();
            b.HasIndex(m => m.ChargeId);
        });
    }

    private static void MapMoney<TOwner>(OwnedNavigationBuilder<TOwner, Money> m)
        where TOwner : class
    {
        m.Property(x => x.Amount).HasColumnName("Amount").HasPrecision(18, 2);
        m.Property(x => x.CurrencyCode).HasColumnName("CurrencyCode").HasMaxLength(3);
    }
}
