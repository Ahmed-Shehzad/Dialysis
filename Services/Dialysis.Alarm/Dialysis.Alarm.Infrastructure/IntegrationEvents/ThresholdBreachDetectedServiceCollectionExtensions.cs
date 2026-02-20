using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Transponder.Transports.Abstractions;

namespace Dialysis.Alarm.Infrastructure.IntegrationEvents;

/// <summary>
/// Registers ASB receive endpoint and handler for ThresholdBreachDetectedIntegrationEvent.
/// Call when AzureServiceBus:ConnectionString is configured.
/// </summary>
public static class ThresholdBreachDetectedServiceCollectionExtensions
{
    public static IServiceCollection AddThresholdBreachDetectedReceiveEndpoint(
        this IServiceCollection services,
        Uri alarmBusAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(alarmBusAddress);

        services.TryAddScoped<ThresholdBreachDetectedReceiveHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReceiveEndpoint>(sp =>
            new ThresholdBreachDetectedReceiveEndpoint(
                alarmBusAddress,
                sp.GetRequiredService<ITransportHostProvider>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ThresholdBreachDetectedReceiveEndpoint>>())));

        return services;
    }
}
