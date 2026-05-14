using System.Collections.Immutable;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Inbound;

/// <summary>
/// Default <see cref="IInboundMessageFactory"/> using version-7 message ids and optional <see cref="TimeProvider"/> for receive time.
/// </summary>
public sealed class DefaultInboundMessageFactory(TimeProvider? time = null) : IInboundMessageFactory
{
    public IntegrationMessage Create(
        Guid flowId,
        ReadOnlyMemory<byte> payload,
        PayloadFormat payloadFormat,
        string? correlationId,
        IReadOnlyDictionary<string, string>? metadata = null,
        DateTimeOffset? receivedAtUtc = null)
    {
        var received = receivedAtUtc ?? (time?.GetUtcNow() ?? DateTimeOffset.UtcNow);
        var cid = string.IsNullOrWhiteSpace(correlationId) ? Guid.CreateVersion7().ToString("N") : correlationId.Trim();
        return new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = flowId,
            CorrelationId = cid,
            Payload = payload,
            PayloadFormat = payloadFormat,
            Metadata = metadata is null
                ? ImmutableDictionary<string, string>.Empty
                : metadata.ToImmutableDictionary(StringComparer.Ordinal),
            ReceivedAtUtc = received,
        };
    }
}

/// <summary>DI registration for default inbound message factory and transport.</summary>
public static class InboundAbstractionsServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultInboundMessageFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IInboundMessageFactory>(sp =>
            new DefaultInboundMessageFactory(sp.GetService<TimeProvider>()));
        return services;
    }

    /// <summary>
    /// Registers <see cref="IInboundTransport"/> with optional preflight using <see cref="IIntegrationFlowRepository"/> when registered.
    /// </summary>
    public static IServiceCollection AddSmartConnectInboundTransport(this IServiceCollection services)
    {
        services.AddScoped<IInboundTransport>(sp =>
            new InboundTransport(
                sp.GetRequiredService<IFlowRuntime>(),
                sp.GetService<IIntegrationFlowRepository>()));
        return services;
    }
}
