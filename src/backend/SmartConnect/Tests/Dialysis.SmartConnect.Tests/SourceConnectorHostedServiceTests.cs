using System.Collections.Immutable;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class SourceConnectorHostedServiceTests
{
    private sealed class CapturingConnector : ISourceConnector
    {
        public string Kind => "test-capture";

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SourceConnectorContext? CapturedContext { get; private set; }

        public Task RunAsync(SourceConnectorContext context, CancellationToken cancellationToken)
        {
            CapturedContext = context;
            Started.TrySetResult();
            return Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }

    private sealed class StubMessageFactory : IInboundMessageFactory
    {
        public IntegrationMessage Create(
            Guid flowId,
            ReadOnlyMemory<byte> payload,
            PayloadFormat format,
            string? correlationId = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            DateTimeOffset? receivedAtUtc = null) =>
            new()
            {
                Id = Guid.NewGuid(),
                FlowId = flowId,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
                Payload = payload,
                PayloadFormat = format,
                Metadata = metadata is null
                    ? ImmutableDictionary<string, string>.Empty
                    : metadata.ToImmutableDictionary(StringComparer.Ordinal),
                ReceivedAtUtc = receivedAtUtc ?? DateTimeOffset.UtcNow,
            };
    }

    private static IHostBuilder BuildHost(CapturingConnector connector, params (string key, string? value)[] config)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cb => cb.AddInMemoryCollection(config.ToDictionary(c => c.key, c => c.value)))
            .ConfigureServices(services =>
            {
                services.AddSingleton<IInboundMessageFactory, StubMessageFactory>();
                services.AddSingleton(connector);
                services.AddSingleton<ISourceConnector>(sp => sp.GetRequiredService<CapturingConnector>());
                services.AddSmartConnectSourceConnectors();
                services.AddSingleton<SourceConnectorRegistry>(sp =>
                {
                    var registry = new SourceConnectorRegistry();
                    registry.Register(sp.GetRequiredService<CapturingConnector>());
                    return registry;
                });
            });
    }

    [Fact]
    public async Task Configured_instance_is_started_with_parameters()
    {
        var connector = new CapturingConnector();
        var flowId = Guid.NewGuid();
        using var host = BuildHost(
            connector,
            ("SmartConnect:SourceConnectors:Instances:0:Name", "alpha"),
            ("SmartConnect:SourceConnectors:Instances:0:Kind", "test-capture"),
            ("SmartConnect:SourceConnectors:Instances:0:DefaultFlowId", flowId.ToString()),
            ("SmartConnect:SourceConnectors:Instances:0:Parameters:foo", "bar")).Build();

        await host.StartAsync().ConfigureAwait(true);
        try
        {
            await connector.Started.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
            Assert.NotNull(connector.CapturedContext);
            Assert.Equal("alpha", connector.CapturedContext!.InstanceName);
            Assert.Equal(flowId, connector.CapturedContext.DefaultFlowId);
            Assert.Equal("bar", connector.CapturedContext.Parameters["foo"]);
        }
        finally
        {
            await host.StopAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Disabled_instance_is_skipped()
    {
        var connector = new CapturingConnector();
        var flowId = Guid.NewGuid();
        using var host = BuildHost(
            connector,
            ("SmartConnect:SourceConnectors:Instances:0:Name", "alpha"),
            ("SmartConnect:SourceConnectors:Instances:0:Kind", "test-capture"),
            ("SmartConnect:SourceConnectors:Instances:0:Enabled", "false"),
            ("SmartConnect:SourceConnectors:Instances:0:DefaultFlowId", flowId.ToString())).Build();

        await host.StartAsync().ConfigureAwait(true);
        try
        {
            var started = await Task.WhenAny(connector.Started.Task, Task.Delay(300)).ConfigureAwait(true);
            Assert.NotSame(connector.Started.Task, started);
        }
        finally
        {
            await host.StopAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Unknown_kind_is_skipped_without_throwing()
    {
        var connector = new CapturingConnector();
        var flowId = Guid.NewGuid();
        using var host = BuildHost(
            connector,
            ("SmartConnect:SourceConnectors:Instances:0:Name", "ghost"),
            ("SmartConnect:SourceConnectors:Instances:0:Kind", "no-such-kind"),
            ("SmartConnect:SourceConnectors:Instances:0:DefaultFlowId", flowId.ToString())).Build();

        await host.StartAsync().ConfigureAwait(true);
        await host.StopAsync().ConfigureAwait(true);
        Assert.False(connector.Started.Task.IsCompleted);
    }
}
