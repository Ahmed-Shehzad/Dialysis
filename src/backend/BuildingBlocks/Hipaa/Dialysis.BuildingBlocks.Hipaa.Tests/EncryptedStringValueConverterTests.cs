using Dialysis.BuildingBlocks.Hipaa.Encryption;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Dialysis.BuildingBlocks.Hipaa.Tests;

public sealed class EncryptedStringValueConverterTests
{
    [Fact]
    public void Converts_To_Cipher_On_Write_And_Back_On_Read()
    {
        var protector = new DataProtectionPhiProtector(new EphemeralDataProtectionProvider());
        var converter = new EncryptedStringValueConverter(protector);

        var plaintext = "Jane Doe, DOB 1985-01-05, MRN-7";
        var cipher = converter.ConvertToProvider(plaintext) as string;
        var roundtrip = converter.ConvertFromProvider(cipher!) as string;

        Assert.NotNull(cipher);
        Assert.NotEqual(plaintext, cipher);
        Assert.Equal(plaintext, roundtrip);
    }
}
