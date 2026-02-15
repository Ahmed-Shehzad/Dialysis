using Transponder.Transports.Abstractions;
using Transponder.Transports.Kafka.Abstractions;

namespace Transponder.Transports.Kafka;

/// <summary>
/// Factory for Kafka transport hosts.
/// </summary>
public sealed class KafkaTransportFactory : ITransportFactory
{
    private static readonly IReadOnlyCollection<string> Schemes =
        ["kafka"];

    public string Name => "Kafka";

    public IReadOnlyCollection<string> SupportedSchemes => Schemes;

    public ITransportHost CreateHost(ITransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings is not IKafkaHostSettings kafkaSettings
            ? throw new ArgumentException(
                $"Expected {nameof(IKafkaHostSettings)} but received {settings.GetType().Name}.",
                nameof(settings))
            : (ITransportHost)new KafkaTransportHost(kafkaSettings);
    }
}
