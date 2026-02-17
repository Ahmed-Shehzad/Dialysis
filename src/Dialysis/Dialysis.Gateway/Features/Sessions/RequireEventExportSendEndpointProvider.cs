using Transponder.Abstractions;

namespace Dialysis.Gateway.Features.Sessions;

/// <summary>
/// Stub used when EventExport is not configured. Session completion requires Transponder saga orchestration;
/// throws if a send is attempted without EventExport.
/// </summary>
internal sealed class RequireEventExportSendEndpointProvider : ISendEndpointProvider
{
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "Session completion requires EventExport to be configured. Set EventExport:ConnectionString and EventExport:Topic, or omit session completion in environments without Azure Service Bus.");
}
