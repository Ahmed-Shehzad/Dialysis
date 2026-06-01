using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.ClinicianNotification;

/// <summary>
/// Composition entry-point. <c>AddClinicianNotification()</c> registers the dispatcher; host
/// composition then plugs concrete senders into the same container per environment policy
/// (Twilio in prod, SMTP-only in dev, e.g.).
/// </summary>
public static class ClinicianNotificationServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddClinicianNotification()
        {
            services.TryAddSingleton<IClinicianNotificationDispatcher, ClinicianNotificationDispatcher>();
            return services;
        }

        public IServiceCollection AddClinicianNotificationSender<TSender>()
            where TSender : class, IClinicianNotificationSender
        {
            services.AddSingleton<IClinicianNotificationSender, TSender>();
            return services;
        }
    }
}
