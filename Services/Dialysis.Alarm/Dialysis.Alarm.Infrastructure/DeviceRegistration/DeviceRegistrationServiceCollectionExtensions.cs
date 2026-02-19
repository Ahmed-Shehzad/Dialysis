using Dialysis.Alarm.Application.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Alarm.Infrastructure.DeviceRegistration;

public static class DeviceRegistrationServiceCollectionExtensions
{
    public static IHttpClientBuilder AddDeviceRegistrationClient(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<DeviceApiOptions>(configuration.GetSection(DeviceApiOptions.SectionName));

        return services.AddHttpClient<IDeviceRegistrationClient, DeviceRegistrationClient>((sp, client) =>
        {
            string baseUrl = configuration["DeviceApi:BaseUrl"] ?? "http://localhost:5054";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        });
    }
}
