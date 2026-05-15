using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Direct;

public static class DirectServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="SmtpDirectMessenger"/>. The caller must additionally register an
        /// <see cref="IDirectCertificateResolver"/>, an <see cref="IDirectSmtpRelay"/>, and supply the
        /// sender's signing <c>X509Certificate2</c>.
        /// </summary>
        public IServiceCollection AddDirectMessaging()
        {
            services.AddSingleton<IDirectMessenger, SmtpDirectMessenger>();
            return services;
        }
    }
}
