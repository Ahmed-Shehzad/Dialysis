using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Dialysis.HIE.Tefca.Trust;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Tefca;

public sealed class TrustAnchorParserTests
{
    [Fact]
    public void Parse_Extracts_Subject_Thumbprint_And_Validity()
    {
        var (pem, expectedSubject, expectedThumbprint, notBefore, notAfter) = Generate_Self_Signed_Pem();

        var parsed = TrustAnchorParser.Parse(pem);

        parsed.Subject.ShouldBe(expectedSubject);
        parsed.Thumbprint.ShouldBe(expectedThumbprint);
        // Cert NotBefore/NotAfter ride X509 second-precision; allow a tiny tolerance.
        Math.Abs((parsed.NotBefore - notBefore.UtcDateTime).TotalSeconds).ShouldBeLessThan(2);
        Math.Abs((parsed.NotAfter - notAfter.UtcDateTime).TotalSeconds).ShouldBeLessThan(2);
        parsed.CertificatePem.ShouldBe(pem);
    }

    [Fact]
    public void Parse_Invalid_Pem_Throws_With_Argument_Name()
    {
        var ex = Should.Throw<ArgumentException>(() => TrustAnchorParser.Parse("not a real PEM"));
        ex.ParamName.ShouldBe("certificatePem");
    }

    private static (string Pem, string Subject, string Thumbprint, DateTimeOffset NotBefore, DateTimeOffset NotAfter)
        Generate_Self_Signed_Pem()
    {
        using var rsa = RSA.Create(2048);
        var subject = "CN=Test QHIN Root";
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-1);
        var notAfter = DateTimeOffset.UtcNow.AddYears(1);
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        var pem = certificate.ExportCertificatePem();
        return (pem, certificate.Subject, certificate.Thumbprint, notBefore, notAfter);
    }
}
