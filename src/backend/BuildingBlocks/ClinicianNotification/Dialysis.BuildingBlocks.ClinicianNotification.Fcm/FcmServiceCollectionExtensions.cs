using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.ClinicianNotification.Fcm;

public static class FcmServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddFcmPushSender(
            IConfiguration configuration,
            string sectionName = "ClinicianNotification:Fcm")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddHttpClient();
            services.AddOptions<FcmPushOptions>().Bind(configuration.GetSection(sectionName));
            services.AddSingleton<IClinicianNotificationSender, FcmPushSender>();
            return services;
        }
    }
}
