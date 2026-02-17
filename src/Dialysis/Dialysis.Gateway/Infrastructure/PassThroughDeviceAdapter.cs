using Dialysis.SharedKernel.Abstractions;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Pass-through adapter for standard JSON input. Phase 1.1.4.
/// Use when device already outputs PDMS-compatible JSON.
/// </summary>
public sealed class PassThroughDeviceAdapter : IDeviceMessageAdapter
{
    public string AdapterId => "passthrough";

    public bool CanHandle(string rawMessage) => true;

    public Task<string?> TransformToVitalsJsonAsync(string rawMessage, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(rawMessage);
}
