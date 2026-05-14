using System.Text;
using Dialysis.BuildingBlocks.Transponder.Serialization;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

internal static class TransponderOutboxRelayPublish
{
    public static async Task PublishRowAsync(
        ITransponderBus bus,
        IMessageSerializer serializer,
        TransponderOutboxMessageEntity row,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(row);

        var messageType = Type.GetType(row.AssemblyQualifiedEventType, throwOnError: false, ignoreCase: false);
        if (messageType is null || !messageType.IsClass || messageType == typeof(string))
            throw new InvalidOperationException($"Cannot resolve outbox message type '{row.AssemblyQualifiedEventType}'.");

        var body = Encoding.UTF8.GetBytes(row.PayloadJson);
        var deserialized = serializer.Deserialize(messageType, body);
        if (deserialized is null)
            throw new InvalidOperationException($"Outbox row {row.Id} deserialized to null.");

        var routingKey = messageType.FullName ?? messageType.Name;
        var correlation = string.IsNullOrWhiteSpace(row.CorrelationId) ? row.Id.ToString("N") : row.CorrelationId!;
        await bus
            .PublishPreparedAsync(routingKey, deserialized, new TransponderPublishOptions(correlation), cancellationToken)
            .ConfigureAwait(false);
    }
}
