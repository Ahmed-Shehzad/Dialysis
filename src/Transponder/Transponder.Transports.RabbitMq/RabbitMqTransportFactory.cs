using Transponder.Transports.Abstractions;
using Transponder.Transports.RabbitMq.Abstractions;

namespace Transponder.Transports.RabbitMq;

/// <summary>
/// Factory for RabbitMQ transport hosts.
/// </summary>
public sealed class RabbitMqTransportFactory : ITransportFactory
{
    private static readonly IReadOnlyCollection<string> Schemes =
        ["rabbitmq", "amqp", "amqps"];

    public string Name => "RabbitMQ";

    public IReadOnlyCollection<string> SupportedSchemes => Schemes;

    public ITransportHost CreateHost(ITransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings is not IRabbitMqHostSettings rabbitSettings
            ? throw new ArgumentException(
                $"Expected {nameof(IRabbitMqHostSettings)} but received {settings.GetType().Name}.",
                nameof(settings))
            : (ITransportHost)new RabbitMqTransportHost(rabbitSettings);
    }
}
