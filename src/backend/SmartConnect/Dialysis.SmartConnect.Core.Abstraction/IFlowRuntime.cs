namespace Dialysis.SmartConnect;

/// <summary>
/// Executes an <see cref="IntegrationMessage"/> through a started <see cref="IntegrationFlow"/>.
/// </summary>
public interface IFlowRuntime
{
    Task<FlowDispatchResult> DispatchAsync(IntegrationMessage message, CancellationToken cancellationToken);
}

public sealed class FlowDispatchResult
{
    public required bool Succeeded { get; init; }

    public string? Error { get; init; }

    public IReadOnlyList<int> OutboundRoutesAttempted { get; init; } = Array.Empty<int>();
}
