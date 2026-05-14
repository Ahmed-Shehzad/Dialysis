using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>
/// Resolves configured <see cref="SourceConnectorInstanceOptions"/> entries against the
/// <see cref="ISourceConnectorRegistry"/> and runs each <see cref="ISourceConnector"/> until host shutdown.
/// One instance failing or returning early does not affect peers.
/// </summary>
public sealed class SourceConnectorHostedService(
    IOptionsMonitor<SourceConnectorHostOptions> options,
    ISourceConnectorRegistry registry,
    IEnumerable<ISourceConnector> registeredConnectors,
    IInboundMessageFactory messageFactory,
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory,
    ILogger<SourceConnectorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Eagerly resolve every registered ISourceConnector so that AddSourceConnector<T>'s
        // factory lambda runs and inserts the connector into the registry before lookup.
        foreach (var connector in registeredConnectors)
        {
            _ = connector;
        }

        var instances = options.CurrentValue.Instances;
        if (instances.Count == 0)
        {
            logger.LogInformation("SmartConnect SourceConnectorHostedService has no instances configured.");
            return;
        }

        var tasks = new List<Task>(instances.Count);
        foreach (var instance in instances)
        {
            if (!instance.Enabled)
            {
                logger.LogInformation("Source connector instance '{Name}' is disabled.", instance.Name);
                continue;
            }

            if (string.IsNullOrWhiteSpace(instance.Kind))
            {
                logger.LogWarning("Source connector instance '{Name}' has empty Kind; skipping.", instance.Name);
                continue;
            }

            if (instance.DefaultFlowId == Guid.Empty)
            {
                logger.LogWarning(
                    "Source connector instance '{Name}' (kind {Kind}) has empty DefaultFlowId; skipping.",
                    instance.Name,
                    instance.Kind);
                continue;
            }

            var connector = registry.TryResolve(instance.Kind);
            if (connector is null)
            {
                logger.LogWarning(
                    "Source connector kind '{Kind}' (instance '{Name}') is not registered; skipping.",
                    instance.Kind,
                    instance.Name);
                continue;
            }

            tasks.Add(RunInstanceAsync(connector, instance, stoppingToken));
        }

        if (tasks.Count == 0)
        {
            return;
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task RunInstanceAsync(
        ISourceConnector connector,
        SourceConnectorInstanceOptions instance,
        CancellationToken stoppingToken)
    {
        var instanceName = string.IsNullOrWhiteSpace(instance.Name) ? connector.Kind : instance.Name;
        var instanceLogger = loggerFactory.CreateLogger($"SmartConnect.SourceConnectors.{connector.Kind}.{instanceName}");
        var ctx = new SourceConnectorContext(
            instanceName,
            instance.DefaultFlowId,
            instance.Parameters,
            messageFactory,
            DispatchAsync,
            instanceLogger);

        instanceLogger.LogInformation(
            "Starting source connector '{Name}' (kind {Kind}) for flow {FlowId}.",
            instanceName,
            connector.Kind,
            instance.DefaultFlowId);

        try
        {
            await connector.RunAsync(ctx, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            instanceLogger.LogError(
                ex,
                "Source connector '{Name}' (kind {Kind}) terminated with an error.",
                instanceName,
                connector.Kind);
        }
        finally
        {
            instanceLogger.LogInformation(
                "Source connector '{Name}' (kind {Kind}) stopped.",
                instanceName,
                connector.Kind);
        }
    }

    private async Task<InboundReceiveResult> DispatchAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var transport = scope.ServiceProvider.GetRequiredService<IInboundTransport>();
        return await transport.DispatchAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
