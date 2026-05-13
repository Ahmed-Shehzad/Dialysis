using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Configurations;

internal static class RegistrationConfiguration
{
    private const string Schema = "ehr_registration";

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(b =>
        {
            b.ToTable("Patients", Schema);
            b.HasKey(p => p.Id);
            b.Property(p => p.MedicalRecordNumber).IsRequired().HasMaxLength(64);
            b.HasIndex(p => p.MedicalRecordNumber).IsUnique();
            b.Property(p => p.DateOfBirth).IsRequired();
            b.Property(p => p.SexAtBirthCode).HasMaxLength(16);
            b.Property(p => p.PreferredLanguageCode).HasMaxLength(16);
            b.Property(p => p.Status).HasConversion<int>().IsRequired();
            b.OwnsOne(p => p.Name, n =>
            {
                n.Property(x => x.FamilyName).HasColumnName("FamilyName").HasMaxLength(128).IsRequired();
                n.Property(x => x.GivenName).HasColumnName("GivenName").HasMaxLength(128).IsRequired();
                n.Property(x => x.MiddleName).HasColumnName("MiddleName").HasMaxLength(128);
                n.Property(x => x.PrefixName).HasColumnName("PrefixName").HasMaxLength(32);
                n.Property(x => x.SuffixName).HasColumnName("SuffixName").HasMaxLength(32);
            });
            b.OwnsOne(p => p.PrimaryAddress, a =>
            {
                a.Property(x => x.Line1).HasColumnName("AddressLine1").HasMaxLength(256);
                a.Property(x => x.Line2).HasColumnName("AddressLine2").HasMaxLength(256);
                a.Property(x => x.City).HasColumnName("AddressCity").HasMaxLength(128);
                a.Property(x => x.StateOrProvince).HasColumnName("AddressState").HasMaxLength(128);
                a.Property(x => x.PostalCode).HasColumnName("AddressPostalCode").HasMaxLength(32);
                a.Property(x => x.CountryCode).HasColumnName("AddressCountryCode").HasMaxLength(2);
            });
            b.Ignore(p => p.ContactPoints);
            b.OwnsMany<Registration.Domain.ContactPoint>("_contactPoints", c =>
            {
                c.ToTable("PatientContactPoints", Schema);
                c.WithOwner().HasForeignKey("PatientId");
                c.Property<int>("Id").ValueGeneratedOnAdd();
                c.HasKey("PatientId", "Id");
                c.Property(x => x.System).HasConversion<int>().IsRequired();
                c.Property(x => x.Value).HasMaxLength(256).IsRequired();
                c.Property(x => x.Use).HasConversion<int>().IsRequired();
            });
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<Provider>(b =>
        {
            b.ToTable("Providers", Schema);
            b.HasKey(p => p.Id);
            b.Property(p => p.NationalProviderIdentifier).HasMaxLength(10).IsRequired();
            b.HasIndex(p => p.NationalProviderIdentifier).IsUnique();
            b.Property(p => p.Kind).HasConversion<int>().IsRequired();
            b.Property(p => p.SpecialtyCode).HasMaxLength(64);
            b.Property(p => p.LicenseNumber).HasMaxLength(64);
            b.OwnsOne(p => p.Name, n =>
            {
                n.Property(x => x.FamilyName).HasColumnName("FamilyName").HasMaxLength(128).IsRequired();
                n.Property(x => x.GivenName).HasColumnName("GivenName").HasMaxLength(128).IsRequired();
                n.Property(x => x.MiddleName).HasColumnName("MiddleName").HasMaxLength(128);
                n.Property(x => x.PrefixName).HasColumnName("PrefixName").HasMaxLength(32);
                n.Property(x => x.SuffixName).HasColumnName("SuffixName").HasMaxLength(32);
            });
            ModuleDbContextBase.MapAuditShadow(b);
        });
    }
}
