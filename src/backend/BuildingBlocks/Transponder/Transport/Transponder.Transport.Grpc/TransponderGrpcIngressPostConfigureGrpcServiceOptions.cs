using Grpc.AspNetCore.Server;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

internal sealed class TransponderGrpcIngressPostConfigureGrpcServiceOptions : IPostConfigureOptions<GrpcServiceOptions>
{
    private readonly IOptions<TransponderGrpcIngressOptions> _ingressOptions;
    public TransponderGrpcIngressPostConfigureGrpcServiceOptions(IOptions<TransponderGrpcIngressOptions> ingressOptions) => _ingressOptions = ingressOptions;
    public void PostConfigure(string? name, GrpcServiceOptions options)
    {
        var o = _ingressOptions.Value;
        options.MaxReceiveMessageSize = o.MaxReceiveMessageSizeBytes;
        options.MaxSendMessageSize = o.MaxSendMessageSizeBytes;
        options.EnableDetailedErrors = o.EnableDetailedErrors;
    }
}
