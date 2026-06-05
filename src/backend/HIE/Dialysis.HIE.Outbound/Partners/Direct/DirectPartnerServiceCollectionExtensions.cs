using System.Security.Cryptography.X509Certificates;
using Dialysis.BuildingBlocks.Direct;
using Dialysis.HIE.Core.Abstraction.Partners;
using Dialysis.HIE.Outbound.Partners.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Outbound.Partners.Direct;

public static class DirectPartnerServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Wires Direct secure messaging when <c>Hie:Direct</c> is configured (sender cert + pickup
        /// directory), then registers a <see cref="DirectPartnerEndpoint"/> for every
        /// <c>Hie:Partners:&lt;id&gt;</c> whose <c>Transport</c> is <c>Direct</c>. Direct partners are
        /// skipped by the HTTP registration so each partner has exactly one endpoint.
        /// </summary>
        public IServiceCollection AddHieDirectMessaging(IConfiguration configuration)
        {
            var options = new DirectMessagingOptions();
            configuration.GetSection(DirectMessagingOptions.SectionName).Bind(options);
            services.Configure<DirectMessagingOptions>(configuration.GetSection(DirectMessagingOptions.SectionName));

            if (!options.IsConfigured)
                return services; // No Direct infra → Direct-transport partners simply have no endpoint.

            // S/MIME object graph: recipient cert resolution, the pickup-directory relay, the sender
            // signing cert, and the messenger itself.
            services.AddSingleton<IDirectCertificateResolver, ConfigDirectCertificateResolver>();
            services.AddSingleton<IDirectSmtpRelay, PickupDirectoryDirectSmtpRelay>();
            services.AddSingleton(_ => LoadSenderCertificate(options));
            services.AddDirectMessaging();

            foreach (var (partnerId, partner) in ReadDirectPartners(configuration))
            {
                var capturedId = partnerId;
                var from = partner.DirectFromAddress!;
                var to = partner.DirectToAddress!;
                services.AddSingleton<IPartnerEndpoint>(sp => new DirectPartnerEndpoint(
                    capturedId,
                    from,
                    to,
                    sp.GetRequiredService<IDirectMessenger>(),
                    sp.GetRequiredService<ILogger<DirectPartnerEndpoint>>()));
            }

            return services;
        }
    }

    private static X509Certificate2 LoadSenderCertificate(DirectMessagingOptions options)
    {
        var pfx = Convert.FromBase64String(options.SenderCertificateBase64!);
        return X509CertificateLoader.LoadPkcs12(pfx, options.SenderCertificatePassword);
    }

    private static IEnumerable<(string PartnerId, PartnerHttpOptions Options)> ReadDirectPartners(IConfiguration configuration)
    {
        foreach (var child in configuration.GetSection("Hie:Partners").GetChildren())
        {
            var partner = child.Get<PartnerHttpOptions>();
            if (partner is null
                || partner.Transport != PartnerTransport.Direct
                || string.IsNullOrWhiteSpace(partner.DirectFromAddress)
                || string.IsNullOrWhiteSpace(partner.DirectToAddress))
            {
                continue;
            }
            yield return (child.Key, partner);
        }
    }
}
