using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

internal sealed class TransponderSignalRIngressPostConfigureHubOptions : IPostConfigureOptions<HubOptions>
{
    private readonly IOptions<TransponderSignalRIngressOptions> _ingressOptions;
    public TransponderSignalRIngressPostConfigureHubOptions(IOptions<TransponderSignalRIngressOptions> ingressOptions) => _ingressOptions = ingressOptions;
    public void PostConfigure(string? name, HubOptions options) => options.MaximumReceiveMessageSize = _ingressOptions.Value.MaximumReceiveMessageSizeBytes;
}
