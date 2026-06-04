using System.Text;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Scheduling.Hangfire;

/// <summary>Hangfire worker entry: deserializes <see cref="TransponderScheduledEnvelope"/> and calls <see cref="ITransponderBus.PublishPreparedAsync"/>.</summary>
public sealed class TransponderHangfirePublishJob
{
    private readonly ITransponderBus _bus;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<TransponderHangfirePublishJob> _logger;
    /// <summary>Hangfire worker entry: deserializes <see cref="TransponderScheduledEnvelope"/> and calls <see cref="ITransponderBus.PublishPreparedAsync"/>.</summary>
    public TransponderHangfirePublishJob(ITransponderBus bus,
        IMessageSerializer serializer,
        ILogger<TransponderHangfirePublishJob> logger)
    {
        _bus = bus;
        _serializer = serializer;
        _logger = logger;
    }
    public async Task ExecuteAsync(TransponderScheduledEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var type = Type.GetType(envelope.AssemblyQualifiedMessageTypeName, throwOnError: false, ignoreCase: false);
        if (type is null)
        {
            _logger.LogError("Transponder Hangfire job: unknown type {TypeName}", envelope.AssemblyQualifiedMessageTypeName);
            throw new InvalidOperationException($"Could not load type '{envelope.AssemblyQualifiedMessageTypeName}'.");
        }

        var bytes = Encoding.UTF8.GetBytes(envelope.JsonPayload);
        var body = _serializer.Deserialize(type, bytes);
        if (body is null)
        {
            _logger.LogError("Transponder Hangfire job: deserialization returned null for {TypeName}", type.FullName);
            throw new InvalidOperationException("Deserialized message was null.");
        }

        var routingKey = type.FullName ?? type.Name;
        await _bus
            .PublishPreparedAsync(
                routingKey,
                body,
                new TransponderPublishOptions(envelope.CorrelationId, envelope.DeduplicationId))
            .ConfigureAwait(false);
    }
}
