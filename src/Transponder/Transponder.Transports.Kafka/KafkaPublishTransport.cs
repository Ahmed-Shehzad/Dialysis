using Confluent.Kafka;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Kafka;

internal sealed class KafkaPublishTransport : IPublishTransport
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly string _topicName;

    public KafkaPublishTransport(IProducer<string, byte[]> producer, string topicName)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
    }

    public async Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var kafkaMessage = new Message<string, byte[]>
        {
            Key = message.MessageId?.ToString() ?? string.Empty,
            Value = message.Body.ToArray(),
            Headers = KafkaTransportHeaders.BuildHeaders(message)
        };

        _ = await _producer.ProduceAsync(_topicName, kafkaMessage, cancellationToken).ConfigureAwait(false);
    }
}
