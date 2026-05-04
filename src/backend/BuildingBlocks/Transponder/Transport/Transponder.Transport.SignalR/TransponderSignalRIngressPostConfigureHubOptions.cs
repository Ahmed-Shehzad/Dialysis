using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

internal sealed class TransponderSignalRIngressPostConfigureHubOptions(
    IOptions<TransponderSignalRIngressOptions> ingressOptions) : IPostConfigureOptions<HubOptions>
{
    public void PostConfigure(string? name, HubOptions options)
    {
        options.MaximumReceiveMessageSize = ingressOptions.Value.MaximumReceiveMessageSizeBytes;
    }
}
