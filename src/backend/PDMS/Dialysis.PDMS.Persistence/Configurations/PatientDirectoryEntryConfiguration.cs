using Dialysis.PDMS.Reporting.Directory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.PDMS.Persistence.Configurations;

/// <summary>
/// EF mapping for the <see cref="PatientDirectoryEntry"/> demographics cache. The key is the EHR
/// patient id (app-set), so it is <see cref="PropertyBuilder.ValueGeneratedNever"/> — otherwise EF
/// treats a re-inserted id as an update and never writes the row.
/// </summary>
internal sealed class PatientDirectoryEntryConfiguration : IEntityTypeConfiguration<PatientDirectoryEntry>
{
    public void Configure(EntityTypeBuilder<PatientDirectoryEntry> builder)
    {
        builder.ToTable("PatientDirectory", "pdms_directory");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MedicalRecordNumber).HasMaxLength(64).IsRequired();
        builder.Property(e => e.GivenName).HasMaxLength(256).IsRequired();
        builder.Property(e => e.FamilyName).HasMaxLength(256).IsRequired();
        builder.Property(e => e.DateOfBirth);
        builder.Property(e => e.UpdatedAtUtc).IsRequired();
        builder.Ignore(e => e.DisplayName);
    }
}
