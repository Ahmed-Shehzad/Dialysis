using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.ClinicianNotification.Twilio;

/// <summary>
/// Composition entry-point. Operators wire Twilio per environment via
/// <c>ClinicianNotification:Twilio:AccountSid</c> / <c>:AuthToken</c> / <c>:FromNumber</c>.
/// Secrets must come from a vault-backed configuration source in production.
/// </summary>
public static class TwilioServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTwilioSmsSender(
            IConfiguration configuration,
            string sectionName = "ClinicianNotification:Twilio")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddHttpClient();
            services.AddOptions<TwilioSmsOptions>().Bind(configuration.GetSection(sectionName));
            services.AddSingleton<IClinicianNotificationSender, TwilioSmsSender>();
            return services;
        }
    }
}
