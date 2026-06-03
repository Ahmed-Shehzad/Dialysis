using Dialysis.BuildingBlocks.Documents.Signing.Csc;
using Dialysis.BuildingBlocks.Documents.Signing.Ltv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Documents.Signing;

/// <summary>DI registration helpers for <see cref="IPdfSigner"/> and its resolvers.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="PdfSharpPdfSigner"/> + the platform / user resolvers + the
    /// LTV augmenter pieces. Hosts that need the eIDAS-QES path also call
    /// <see cref="AddEidasQesSigning"/>.
    /// </summary>
    public static IServiceCollection AddPdfSigning(this IServiceCollection services, IConfiguration documentSigningSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(documentSigningSection);

        services.Configure<PlatformSigningCertificateOptions>(documentSigningSection.GetSection("PlatformCertificate"));
        services.Configure<UserSigningCertificateOptions>(documentSigningSection.GetSection("UserCertificate"));
        services.Configure<TsaOptions>(documentSigningSection.GetSection("Tsa"));
        services.Configure<LtvOptions>(documentSigningSection.GetSection("Ltv"));

        services.AddSingleton<ISigningCertificateResolver, ConfiguredPlatformCertificateResolver>();
        services.AddSingleton<ISigningCertificateResolver, KeycloakUserCertificateResolver>();
        services.AddSingleton<PdfSharpLtvAugmenter>();
        services.AddSingleton<RevocationEvidenceCollector>();
        services.AddSingleton<IPdfSigner, PdfSharpPdfSigner>();
        return services;
    }

    /// <summary>
    /// Adds the eIDAS-QES resolver + the CSC v2 client. Requires the host to register a
    /// named <see cref="HttpClient"/> for <see cref="CscV2Client.HttpClientName"/> (typically
    /// via <c>AddResilientModuleHttpClient</c>) and an <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
    /// + <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>.
    /// </summary>
    public static IServiceCollection AddEidasQesSigning(this IServiceCollection services, IConfiguration documentSigningSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(documentSigningSection);
        services.Configure<CscV2Options>(documentSigningSection.GetSection("Tsp"));
        services.AddMemoryCache();
        services.AddSingleton<CscV2Client>(sp => new CscV2Client(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(CscV2Client.HttpClientName),
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CscV2Options>>(),
            sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CscV2Client>>()));
        services.AddSingleton<IRemoteSignatureService, CscV2RemoteSignatureService>();
        services.AddSingleton<ISigningCertificateResolver, TspQesCertificateResolver>();
        return services;
    }
}
