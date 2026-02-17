using Dialysis.SharedKernel.Abstractions;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// No-op event export. Phase 2.3.2. Replace with Kafka/HTTP publisher when needed.
/// </summary>
public sealed class NoOpEventExportPublisher : IEventExportPublisher
{
    public Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
