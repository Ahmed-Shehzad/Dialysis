using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.ClinicianNotification.Apns;

public static class ApnsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApnsPushSender(
            IConfiguration configuration,
            string sectionName = "ClinicianNotification:Apns")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddHttpClient();
            services.AddOptions<ApnsPushOptions>().Bind(configuration.GetSection(sectionName));
            services.AddSingleton<IClinicianNotificationSender, ApnsPushSender>();
            return services;
        }
    }
}
