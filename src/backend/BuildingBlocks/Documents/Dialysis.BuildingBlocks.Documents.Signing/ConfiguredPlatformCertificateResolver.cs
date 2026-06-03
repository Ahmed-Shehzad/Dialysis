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
                throw new InvalidOperationException(
                    "Documents:Signing:PlatformCertificate:PfxPath is not configured.");
            }
            return X509CertificateLoader.LoadPkcs12FromFile(opts.PfxPath, opts.PfxPassword ?? string.Empty);
        });
    }

    public PdfSigningCertificateSource Source => PdfSigningCertificateSource.Platform;

    public Task<X509Certificate2> ResolveAsync(PdfSigningRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(_certificate.Value);
}
