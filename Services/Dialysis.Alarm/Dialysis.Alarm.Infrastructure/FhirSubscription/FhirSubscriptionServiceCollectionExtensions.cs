using BuildingBlocks.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Refit;

namespace Dialysis.Alarm.Infrastructure.FhirSubscription;

public static class FhirSubscriptionServiceCollectionExtensions
{
    public static IServiceCollection AddFhirSubscriptionNotifyClient(this IServiceCollection services, IConfiguration configuration)
    {
        string baseUrl = configuration["FhirSubscription:NotifyUrl"] ?? "http://localhost";
        _ = services.AddRefitClient<IFhirSubscriptionNotifyApi>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"));

        return services.AddScoped<IFhirSubscriptionNotifyClient, FhirSubscriptionNotifyClient>();
    }
}
