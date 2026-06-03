using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Documents.Signing;

/// <summary>DI registration helpers for <see cref="IPdfSigner"/> and its resolvers.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="PdfSharpPdfSigner"/> + both stock resolvers. Hosts that need
    /// only one mode can add the resolvers individually instead.
    /// </summary>
    public static IServiceCollection AddPdfSigning(this IServiceCollection services, IConfiguration documentSigningSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(documentSigningSection);

        services.Configure<PlatformSigningCertificateOptions>(documentSigningSection.GetSection("PlatformCertificate"));
        services.Configure<UserSigningCertificateOptions>(documentSigningSection.GetSection("UserCertificate"));

        services.AddSingleton<ISigningCertificateResolver, ConfiguredPlatformCertificateResolver>();
        services.AddSingleton<ISigningCertificateResolver, KeycloakUserCertificateResolver>();
        services.AddSingleton<IPdfSigner, PdfSharpPdfSigner>();
        return services;
    }
}
