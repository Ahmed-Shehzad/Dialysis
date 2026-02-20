using Dialysis.Treatment.Application.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using Refit;

namespace Dialysis.Treatment.Infrastructure.DeviceRegistration;

public static class DeviceRegistrationServiceCollectionExtensions
{
    public static IServiceCollection AddDeviceRegistrationClient(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<DeviceApiOptions>(configuration.GetSection(DeviceApiOptions.SectionName));

        _ = services.AddRefitClient<IDeviceApi>()
            .ConfigureHttpClient((_, client) =>
            {
                string baseUrl = configuration["DeviceApi:BaseUrl"] ?? "http://localhost:5054";
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            })
            .AddStandardResilienceHandler();

        return services.AddScoped<IDeviceRegistrationClient, DeviceRegistrationClient>();
    }
}

