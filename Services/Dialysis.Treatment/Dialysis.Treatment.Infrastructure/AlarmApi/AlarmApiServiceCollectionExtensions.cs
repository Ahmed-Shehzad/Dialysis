using Dialysis.Treatment.Application.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using Refit;

namespace Dialysis.Treatment.Infrastructure.AlarmApi;

public static class AlarmApiServiceCollectionExtensions
{
    public static IServiceCollection AddAlarmApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<AlarmApiOptions>(configuration.GetSection(AlarmApiOptions.SectionName));

        string? baseUrl = configuration["AlarmApi:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _ = services.AddRefitClient<IAlarmApi>()
                .ConfigureHttpClient((_, client) => client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"))
                .AddStandardResilienceHandler();
        }

        return services.AddScoped<IAlarmApiClient, AlarmApiClient>();
    }
}
