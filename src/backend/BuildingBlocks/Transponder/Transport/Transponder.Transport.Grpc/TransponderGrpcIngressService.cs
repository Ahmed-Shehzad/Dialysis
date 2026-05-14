using System.Threading.Channels;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>gRPC ingress relay: unary publish fans out to all active subscribe streams.</summary>
public sealed class TransponderGrpcIngressService(
    TransponderGrpcIngressHub hub,
    ILogger<TransponderGrpcIngressService> logger,
    IServiceProvider services) : TransponderIngress.TransponderIngressBase
{
    public override async Task<PublishResponse> Publish(TransportEnvelope request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.RoutingKey))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "routing_key is required."));

        await EnsureAuthorizedAsync(context).ConfigureAwait(false);

        if (services.GetService<ITransponderGrpcIngressPublishJournal>() is { } journal)
            await journal.AppendAsync(request, context, context.CancellationToken).ConfigureAwait(false);

        await hub.BroadcastAsync(request, context.CancellationToken).ConfigureAwait(false);
        return new PublishResponse();
    }

    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<TransportEnvelope> responseStream,
        ServerCallContext context)
    {
        await EnsureAuthorizedAsync(context).ConfigureAwait(false);

        var channel = Channel.CreateBounded<TransportEnvelope>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

        using (hub.Register(channel.Writer))
        {
            logger.LogInformation(
                "Transponder gRPC subscribe started ({ClientName})",
                string.IsNullOrEmpty(request.ClientName) ? "anonymous" : request.ClientName);

            try
            {
                await foreach (var env in channel.Reader.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
                    await responseStream.WriteAsync(env, context.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                // normal shutdown
            }
        }
    }

    private async Task EnsureAuthorizedAsync(ServerCallContext context)
    {
        if (services.GetService<ITransponderGrpcIngressAuthorizer>() is not { } authorizer)
            return;

        await authorizer.AuthorizeAsync(context, context.CancellationToken).ConfigureAwait(false);
    }
}
