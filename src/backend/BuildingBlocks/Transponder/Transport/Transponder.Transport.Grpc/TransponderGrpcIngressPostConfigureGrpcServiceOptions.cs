using Grpc.AspNetCore.Server;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

internal sealed class TransponderGrpcIngressPostConfigureGrpcServiceOptions(
    IOptions<TransponderGrpcIngressOptions> ingressOptions) : IPostConfigureOptions<GrpcServiceOptions>
{
    public void PostConfigure(string? name, GrpcServiceOptions options)
    {
        var o = ingressOptions.Value;
        options.MaxReceiveMessageSize = o.MaxReceiveMessageSizeBytes;
        options.MaxSendMessageSize = o.MaxSendMessageSizeBytes;
        options.EnableDetailedErrors = o.EnableDetailedErrors;
    }
}
