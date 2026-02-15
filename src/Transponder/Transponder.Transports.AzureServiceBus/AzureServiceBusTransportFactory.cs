using Transponder.Transports.Abstractions;
using Transponder.Transports.AzureServiceBus.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

/// <summary>
/// Factory for Azure Service Bus transport hosts.
/// </summary>
public sealed class AzureServiceBusTransportFactory : ITransportFactory
{
    private static readonly IReadOnlyCollection<string> Schemes =
        ["sb", "azureservicebus"];

    public string Name => "AzureServiceBus";

    public IReadOnlyCollection<string> SupportedSchemes => Schemes;

    public ITransportHost CreateHost(ITransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings is not IAzureServiceBusHostSettings azureSettings
            ? throw new ArgumentException(
                $"Expected {nameof(IAzureServiceBusHostSettings)} but received {settings.GetType().Name}.",
                nameof(settings))
            : (ITransportHost)new AzureServiceBusTransportHost(azureSettings);
    }
}
