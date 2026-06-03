using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Documents.Signing;

/// <summary>Options for <see cref="KeycloakUserCertificateResolver"/>.</summary>
public sealed class UserSigningCertificateOptions
{
    /// <summary>
    /// Directory containing per-user PFX files named <c>{userId}.pfx</c>. For the v1 flow
    /// the host operator pre-provisions the file (typically out-of-band from Keycloak's
    /// user attributes); a future TSP-backed resolver replaces this lookup without
    /// changing <see cref="ISigningCertificateResolver"/>.
    /// </summary>
    public string? PfxDirectory { get; set; }

    /// <summary>Password used for every per-user PFX (or set per-user via secret store).</summary>
    public string? PfxPassword { get; set; }
}

/// <summary>
/// Resolves a per-user signing cert. The v1 implementation looks up
/// <c>{PfxDirectory}/{userId}.pfx</c>; productionising means swapping this for a Keycloak-
/// attribute-driven or HSM-backed lookup. The resolver interface stays the same.
/// </summary>
public sealed class KeycloakUserCertificateResolver : ISigningCertificateResolver
{
    private readonly UserSigningCertificateOptions _options;
    private readonly ILogger<KeycloakUserCertificateResolver> _logger;

    public KeycloakUserCertificateResolver(
        IOptions<UserSigningCertificateOptions> options,
        ILogger<KeycloakUserCertificateResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _logger = logger;
    }

    public PdfSigningCertificateSource Source => PdfSigningCertificateSource.User;

    public Task<X509Certificate2> ResolveAsync(PdfSigningRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new InvalidOperationException("UserId is required for a per-user PDF signature.");
        }
        if (string.IsNullOrWhiteSpace(_options.PfxDirectory))
        {
            throw new InvalidOperationException(
                "Documents:Signing:UserCertificate:PfxDirectory is not configured.");
        }
        var path = Path.Combine(_options.PfxDirectory, request.UserId + ".pfx");
        if (!File.Exists(path))
        {
            _logger.LogWarning("No signing certificate found for user {UserId} at {Path}.", request.UserId, path);
            throw new InvalidOperationException(
                $"No signing certificate has been provisioned for user '{request.UserId}'.");
        }
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(path, _options.PfxPassword ?? string.Empty);
        return Task.FromResult(certificate);
    }
}
