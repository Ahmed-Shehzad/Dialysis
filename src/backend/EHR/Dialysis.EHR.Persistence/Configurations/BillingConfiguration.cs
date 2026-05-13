using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.EHR.Persistence.Configurations;

internal static class BillingConfiguration
{
    private const string Schema = "ehr_billing";

    public static void Configure(ModelBuilder modelBuilder)
    {
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
    }

    private static void MapMoney<TOwner>(OwnedNavigationBuilder<TOwner, Money> m)
        where TOwner : class
    {
        m.Property(x => x.Amount).HasColumnName("Amount").HasPrecision(18, 2);
        m.Property(x => x.CurrencyCode).HasColumnName("CurrencyCode").HasMaxLength(3);
    }
}
