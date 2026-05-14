using System.Text;
using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Base;

namespace Dialysis.BuildingBlocks.Transponder.Scheduling.TickerQ;

/// <summary>
/// TickerQ entry points for scheduled Transponder publishes. Function name must match <see cref="TransponderTickerQPublishJobs.FunctionName"/>.
/// </summary>
public sealed class TransponderTickerQPublishJobs(
    ITransponderBus bus,
    IMessageSerializer serializer,
    ILogger<TransponderTickerQPublishJobs> logger)
{
    public const string FunctionName = "Transponder.PublishScheduledMessage";

    [TickerFunction(FunctionName)]
    public async Task PublishScheduledMessageAsync(
        TickerFunctionContext<TransponderScheduledEnvelope> context,
        CancellationToken cancellationToken)
    {
        var envelope = context.Request;
        ArgumentNullException.ThrowIfNull(envelope);

        var type = Type.GetType(envelope.AssemblyQualifiedMessageTypeName, throwOnError: false, ignoreCase: false);
        if (type is null)
        {
            logger.LogError("Transponder TickerQ job: unknown type {TypeName}", envelope.AssemblyQualifiedMessageTypeName);
            throw new InvalidOperationException($"Could not load type '{envelope.AssemblyQualifiedMessageTypeName}'.");
        }

        var bytes = Encoding.UTF8.GetBytes(envelope.JsonPayload);
        var body = serializer.Deserialize(type, bytes);
        if (body is null)
        {
            logger.LogError("Transponder TickerQ job: deserialization returned null for {TypeName}", type.FullName);
            throw new InvalidOperationException("Deserialized message was null.");
        }

        var routingKey = type.FullName ?? type.Name;
        await bus
            .PublishPreparedAsync(
                routingKey,
                body,
                new TransponderPublishOptions(envelope.CorrelationId, envelope.DeduplicationId),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
