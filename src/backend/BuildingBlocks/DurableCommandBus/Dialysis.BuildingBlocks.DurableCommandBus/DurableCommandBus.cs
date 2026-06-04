using System.Diagnostics;
using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Default <see cref="IDurableCommandBus"/>. Wraps the command in a <see cref="DurableCommandEnvelope"/>,
/// publishes via the durable <see cref="ITransponderBus"/>, and returns the acceptance token.
/// The publisher confirms enabled on the underlying RabbitMQ transport are what make the
/// "the broker has it" semantic real — if the publish fails or the broker nacks, the bus
/// throws and the API endpoint surfaces 503.
/// </summary>
public sealed class DurableCommandBus(
    ITransponderBus transport,
    IDurableCommandCatalog catalog,
    IOptions<DurableCommandBusOptions> options,
    TimeProvider clock,
    DurableCommandMetrics metrics,
    ILogger<DurableCommandBus> logger) : IDurableCommandBus
{
    private readonly DurableCommandBusOptions _options = options.Value;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<DurableCommandAcceptance> EnqueueAsync<TCommand, TResult>(
        TCommand command,
        Guid? commandId = null,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!catalog.TryGetForType(typeof(TCommand), out var registration))
        {
            throw new DurableCommandException(
                $"{typeof(TCommand).FullName} is not registered in the durable command catalog. "
                + "Call RegisterCommand<TCommand,TResult>() in AddDurableCommandBus.");
        }

        var id = commandId ?? Guid.CreateVersion7();
        var correlationId = id.ToString("N");
        var envelope = new DurableCommandEnvelope(
            CommandId: id,
            CommandTypeKey: registration.CommandTypeKey,
            SchemaVersion: 1,
            PayloadJson: JsonSerializer.Serialize(command, typeof(TCommand), _jsonOptions),
            CorrelationId: correlationId,
            EnqueuedAtUtc: clock.GetUtcNow().UtcDateTime,
            RequestedBySubject: null);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await transport.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Durable command publish failed; module={Module}, commandType={CommandType}, commandId={CommandId}",
                _options.ModuleSlug, registration.CommandTypeKey, id);
            throw new DurableCommandException(
                $"Failed to publish {registration.CommandTypeKey} to the durable transport.", ex);
        }
        finally
        {
            stopwatch.Stop();
            metrics.EnqueueLatencyMilliseconds.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("module", _options.ModuleSlug),
                new KeyValuePair<string, object?>("command_type", registration.CommandTypeKey));
        }

        metrics.CommandsEnqueued.Add(
            1,
            new KeyValuePair<string, object?>("module", _options.ModuleSlug),
            new KeyValuePair<string, object?>("command_type", registration.CommandTypeKey));

        logger.LogInformation(
            "Enqueued durable command {CommandType} (id={CommandId}, correlationId={CorrelationId}) for module {Module}",
            registration.CommandTypeKey, id, correlationId, _options.ModuleSlug);

        return new DurableCommandAcceptance(
            CommandId: id,
            CorrelationId: correlationId,
            StatusEndpoint: $"{_options.StatusEndpointPrefix.TrimEnd('/')}/{correlationId}");
    }
}
