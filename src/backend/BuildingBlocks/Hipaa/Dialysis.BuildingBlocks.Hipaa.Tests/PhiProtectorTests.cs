using Dialysis.BuildingBlocks.Hipaa.Encryption;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Dialysis.BuildingBlocks.Hipaa.Tests;

public sealed class PhiProtectorTests
{
    [Fact]
    public void Round_Trips_Plaintext_Through_Encrypt_And_Decrypt()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = new DataProtectionPhiProtector(provider);

        var plaintext = "MRN-12345";
        var cipher = protector.Encrypt(plaintext);
        var roundtrip = protector.Decrypt(cipher);

        Assert.NotEqual(plaintext, cipher);
        Assert.Equal(plaintext, roundtrip);
    }

    [Fact]
    public void Encryption_Is_Versioned_So_V2_Reader_Can_Coexist() =>
        // The purpose string carries an explicit .v1 suffix. Any future v2 implementation must
        // declare a new purpose so already-encrypted v1 columns remain decryptable until the
        // operator runs a migration. The test pins the literal so any rename breaks the build.
        Assert.Equal("hipaa.phi.column-encryption.v1", DataProtectionPhiProtector.PurposeV1);
}
