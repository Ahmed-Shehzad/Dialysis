namespace Transponder;

public sealed class OutboxMessageFactoryOptions
{
    public Ulid? MessageId { get; init; }

    public Ulid? CorrelationId { get; init; }

    public Ulid? ConversationId { get; init; }

    public Uri? SourceAddress { get; init; }

    public Uri? DestinationAddress { get; init; }

    public IReadOnlyDictionary<string, object?>? Headers { get; init; }
}
