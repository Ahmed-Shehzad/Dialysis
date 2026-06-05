using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Documents.Signing;

/// <summary>Options for <see cref="ConfiguredPlatformCertificateResolver"/>.</summary>
public sealed class PlatformSigningCertificateOptions
{
    /// <summary>Path to a PFX / PKCS#12 file holding the platform cert + private key.</summary>
    public string? PfxPath { get; set; }

    /// <summary>Password protecting <see cref="PfxPath"/>.</summary>
    public string? PfxPassword { get; set; }

    /// <summary>
    /// Development-only escape hatch: when <see cref="PfxPath"/> is unset and this is
    /// <c>true</c>, the resolver mints an in-memory self-signed signing certificate so the
    /// document-signing flow is exercisable in local / demo environments without
    /// provisioning a real SMC-B / organisational certificate. Never enable in production —
    /// the resulting signatures chain to an untrusted, ephemeral key.
    /// </summary>
    public bool DevelopmentSelfSigned { get; set; }
}

/// <summary>
/// Resolves the single organisational / SMC-B signing cert configured on the host. The
/// cert is loaded once and cached; the host is expected to restart on rotation.
/// Mirrors the X509 plumbing used by <c>SmtpDirectMessenger</c>.
/// </summary>
public sealed class ConfiguredPlatformCertificateResolver : ISigningCertificateResolver
{
    private readonly Lazy<X509Certificate2> _certificate;

    public ConfiguredPlatformCertificateResolver(IOptions<PlatformSigningCertificateOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _certificate = new Lazy<X509Certificate2>(() =>
        {
            var opts = options.Value;
            if (string.IsNullOrWhiteSpace(opts.PfxPath))
            {
                if (opts.DevelopmentSelfSigned)
                {
                    return CreateDevelopmentSelfSignedCertificate();
                }
                throw new InvalidOperationException(
                    "Documents:Signing:PlatformCertificate:PfxPath is not configured.");
            }
            return X509CertificateLoader.LoadPkcs12FromFile(opts.PfxPath, opts.PfxPassword ?? string.Empty);
        });
    }

    public PdfSigningCertificateSource Source => PdfSigningCertificateSource.Platform;

    public Task<X509Certificate2> ResolveAsync(PdfSigningRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(_certificate.Value);

    /// <summary>
    /// Mints a throwaway self-signed RSA-3072 signing certificate for local / demo use. The
    /// private key is round-tripped through a PFX export so it is usable for PAdES signing on
    /// every platform. Gated behind <see cref="PlatformSigningCertificateOptions.DevelopmentSelfSigned"/>;
    /// the certificate is untrusted by design and must never reach production.
    /// </summary>
    private static X509Certificate2 CreateDevelopmentSelfSignedCertificate()
    {
        using var rsa = RSA.Create(3072);
        var request = new CertificateRequest(
            "CN=Dialysis Demo Platform Signer (DEVELOPMENT - DO NOT TRUST), O=Dialysis",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
                critical: true));
        var now = DateTimeOffset.UtcNow;
        using var ephemeral = request.CreateSelfSigned(now.AddMinutes(-5), now.AddYears(2));
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), password: null);
    }
}
