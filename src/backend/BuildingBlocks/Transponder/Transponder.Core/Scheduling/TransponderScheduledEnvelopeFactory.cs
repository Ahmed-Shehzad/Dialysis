using System.Text;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>Builds <see cref="TransponderScheduledEnvelope"/> snapshots using <see cref="IMessageSerializer"/>.</summary>
public static class TransponderScheduledEnvelopeFactory
{
    public static TransponderScheduledEnvelope Create<TMessage>(
        TMessage message,
        IMessageSerializer serializer,
        TransponderPublishOptions? publishOptions)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(serializer);
        var type = typeof(TMessage);
        var name = type.AssemblyQualifiedName
            ?? throw new InvalidOperationException($"Type {type.FullName} has no assembly-qualified name; it cannot be scheduled for deferred deserialization.");
        var bytes = serializer.Serialize(message);
        return new TransponderScheduledEnvelope
        {
            AssemblyQualifiedMessageTypeName = name,
            JsonPayload = Encoding.UTF8.GetString(bytes.Span),
            CorrelationId = publishOptions?.CorrelationId,
            DeduplicationId = publishOptions?.DeduplicationId,
        };
    }
}
