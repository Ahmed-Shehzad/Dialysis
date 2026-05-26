using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dialysis.BuildingBlocks.Hipaa.Encryption;

/// <summary>
/// EF Core value converter that transparently encrypts a string column via <see cref="IPhiProtector"/>
/// on write and decrypts it on read. Apply per-property in <c>OnModelCreating</c> on the column that
/// holds PHI; other columns stay plaintext so they remain queryable / indexable.
///
/// Example:
/// <code>
/// modelBuilder.Entity&lt;PatientEntity&gt;().Property(p =&gt; p.MedicalRecordNumber)
///     .HasConversion(new EncryptedStringValueConverter(phiProtector));
/// </code>
/// </summary>
public sealed class EncryptedStringValueConverter : ValueConverter<string, string>
{
    public EncryptedStringValueConverter(IPhiProtector protector)
        : base(
            plain => protector.Encrypt(plain),
            cipher => protector.Decrypt(cipher))
    {
    }
}
