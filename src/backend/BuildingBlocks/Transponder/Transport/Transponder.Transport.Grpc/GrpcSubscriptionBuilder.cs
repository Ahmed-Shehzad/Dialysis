using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Declares which message contracts this application consumes from the gRPC ingress stream.</summary>
public sealed class GrpcSubscriptionBuilder(IServiceCollection services, GrpcSubscriptionRegistry registry)
{
    public GrpcSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(services);
        return this;
    }
}
