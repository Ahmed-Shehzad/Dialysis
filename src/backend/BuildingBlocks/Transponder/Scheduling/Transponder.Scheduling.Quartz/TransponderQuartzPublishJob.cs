using System.Text;
using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Dialysis.BuildingBlocks.Transponder.Scheduling.Quartz;

/// <summary>Quartz <see cref="IJob"/> that deserializes <see cref="TransponderScheduledEnvelope"/> and calls <see cref="ITransponderBus.PublishPreparedAsync"/>.</summary>
public sealed class TransponderQuartzPublishJob : IJob
{
    private readonly ITransponderBus _bus;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<TransponderQuartzPublishJob> _logger;
    /// <summary>Quartz <see cref="IJob"/> that deserializes <see cref="TransponderScheduledEnvelope"/> and calls <see cref="ITransponderBus.PublishPreparedAsync"/>.</summary>
    public TransponderQuartzPublishJob(ITransponderBus bus,
        IMessageSerializer serializer,
        ILogger<TransponderQuartzPublishJob> logger)
    {
        _bus = bus;
        _serializer = serializer;
        _logger = logger;
    }
    internal const string EnvelopeJobDataKey = "transponderEnvelopeJson";

    public async Task Execute(IJobExecutionContext context)
    {
        var json = context.MergedJobDataMap.GetString(EnvelopeJobDataKey);
        if (string.IsNullOrEmpty(json))
        {
            _logger.LogError("Transponder Quartz job {JobKey} missing envelope payload", context.JobDetail.Key);
            throw new InvalidOperationException("Missing envelope job data.");
        }

        var envelope = JsonSerializer.Deserialize<TransponderScheduledEnvelope>(json);
        if (envelope is null)
        {
            _logger.LogError("Transponder Quartz job {JobKey} envelope JSON invalid", context.JobDetail.Key);
            throw new InvalidOperationException("Invalid envelope JSON.");
        }

        var type = Type.GetType(envelope.AssemblyQualifiedMessageTypeName, throwOnError: false, ignoreCase: false);
        if (type is null)
        {
            _logger.LogError("Transponder Quartz job: unknown type {TypeName}", envelope.AssemblyQualifiedMessageTypeName);
            throw new InvalidOperationException($"Could not load type '{envelope.AssemblyQualifiedMessageTypeName}'.");
        }

        var bytes = Encoding.UTF8.GetBytes(envelope.JsonPayload);
        var body = _serializer.Deserialize(type, bytes);
        if (body is null)
        {
            _logger.LogError("Transponder Quartz job: deserialization returned null for {TypeName}", type.FullName);
            throw new InvalidOperationException("Deserialized message was null.");
        }

        var routingKey = type.FullName ?? type.Name;
        await _bus
            .PublishPreparedAsync(
                routingKey,
                body,
                new TransponderPublishOptions(envelope.CorrelationId, envelope.DeduplicationId),
                context.CancellationToken)
            .ConfigureAwait(false);
    }
}
