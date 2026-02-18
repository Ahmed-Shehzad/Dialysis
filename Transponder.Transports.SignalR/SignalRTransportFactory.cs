using Transponder.Transports.Abstractions;
using Transponder.Transports.SignalR.Abstractions;

namespace Transponder.Transports.SignalR;

/// <summary>
/// Factory for SignalR transport hosts.
/// </summary>
public sealed class SignalRTransportFactory : ITransportFactory
{
    private static readonly IReadOnlyCollection<string> Schemes =
        ["signalr", "signalrs"];

    public string Name => "SignalR";

    public IReadOnlyCollection<string> SupportedSchemes => Schemes;

    public ITransportHost CreateHost(ITransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings is not ISignalRHostSettings signalRSettings
            ? throw new ArgumentException(
                $"Expected {nameof(ISignalRHostSettings)} but received {settings.GetType().Name}.",
                nameof(settings))
            : new SignalRTransportHost(signalRSettings);
    }
}
